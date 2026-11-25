#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using ModestTree;
using MonoMod.RuntimeDetour;
using SongCore.Utilities;
using Zenject;

namespace SongCore.Hooks.BeatmapLevelCache
{
    internal class BeatmapDataCacheHooks : IInitializable, IDisposable
    {
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly BeatmapLevelCache _beatmapLevelCache;
        private readonly EventProxyHooks _eventProxyHooks;

        private Hook _replaceDidSelectLevelEventHook = null!;
        private Hook _replaceDidChangeContentEventHook = null!;
        private Hook _createbeatmapKeyHook = null!;
        private CancellationToken _cancellationToken;

        private BeatmapDataCacheHooks(CustomLevelLoader customLevelLoader, BeatmapLevelsModel beatmapLevelsModel, BeatmapLevelCache beatmapLevelCache, EventProxyHooks eventProxyHooks)
        {
            _customLevelLoader = customLevelLoader;
            _beatmapLevelsModel = beatmapLevelsModel;
            _beatmapLevelCache = beatmapLevelCache;
            _eventProxyHooks = eventProxyHooks;
        }

        public void Initialize()
        {
            _replaceDidSelectLevelEventHook = new Hook(typeof(LevelCollectionViewController).GetMethod(nameof(LevelCollectionViewController.DidActivate), BindingFlags.Instance | BindingFlags.NonPublic)!, ReplaceDidSelectLevelEvent, true);
            _replaceDidChangeContentEventHook = new Hook(typeof(StandardLevelDetailViewController).GetMethod(nameof(StandardLevelDetailViewController.DidActivate), BindingFlags.Instance | BindingFlags.NonPublic)!, ReplaceDidChangeContentEvent, true);
            _createbeatmapKeyHook = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.RefreshContent), BindingFlags.Instance | BindingFlags.Public)!, SetBeatmapLevelCacheBeatmapKey, true);
        }

        public void Dispose()
        {
            _replaceDidSelectLevelEventHook.Dispose();
            _replaceDidChangeContentEventHook.Dispose();
            _createbeatmapKeyHook.Dispose();
        }

        private async Task<IBeatmapLevelData?> InitializeBeatmapLevelCacheAsync(BeatmapLevel beatmapLevel, CancellationToken cancellationToken)
        {
            try
            {
                var beatmapLevelDataResult = await _beatmapLevelsModel.GetBeatmapLevelAsync(beatmapLevel.levelID, cancellationToken);

                if (beatmapLevelDataResult.isError)
                {
                    Plugin.Log.Error($"Failed to load beatmap level data for {beatmapLevel.levelID}");

                    return null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                Plugin.Log.Debug($"Initialized beatmap level cache for {beatmapLevel.levelID}");

                if (_beatmapLevelCache.BeatmapLevel != beatmapLevel) return null;
                return _beatmapLevelCache.BeatmapLevelData = beatmapLevelDataResult.beatmapLevel.beatmapLevelData;
            }
            catch (OperationCanceledException)
            {
                Plugin.Log.Debug($"Beatmap level cache initialization was cancelled for {beatmapLevel.levelID}");

                return null;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"An error occurred during beatmap level cache initialization.\n{ex}");

                return null;
            }
        }

        private async Task InvokeEventAsync<TSender, TArgs>(TSender sender, TArgs? eventArgs, Delegate? originalDelegate, CancellationToken cancellationToken)
        {
            if (originalDelegate == null)
            {
                return;
            }

            var baseHandlers = new List<Action<TSender, TArgs?>>();
            var modHandlers = new List<Action<TSender, TArgs?>>();

            foreach (var handler in originalDelegate.GetInvocationList().Cast<Action<TSender, TArgs?>>())
            {
                if (handler.Method.DeclaringType!.Assembly == typeof(LevelCollectionTableView).Assembly ||
                    Directory.GetParent(handler.Method.DeclaringType.Assembly.Location)!.Name == "Managed")
                {
                    baseHandlers.Add(handler);
                }
                else
                {
                    modHandlers.Add(handler);
                }
            }

            foreach (var action in baseHandlers)
            {
                action(sender, eventArgs);
            }

            await _beatmapLevelCache.WaitUntilReadyAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            foreach (var action in modHandlers)
            {
                action(sender, eventArgs);
            }
        }

        private async void HandleDidSelectLevel(LevelCollectionTableView levelCollectionTableView, BeatmapLevel beatmapLevel)
        {
            Plugin.Log.Trace("LevelCollectionTableView.didSelectLevelEvent");

            var message = $"Selected level: {beatmapLevel.levelID} | {beatmapLevel.songName}";

            Plugin.Log.Debug(message);

            _beatmapLevelCache.Init(beatmapLevel, InitializeBeatmapLevelCacheAsync);
            _cancellationToken = _beatmapLevelCache.CancellationTokenSource!.Token;

            await InvokeEventAsync(levelCollectionTableView, beatmapLevel, _eventProxyHooks.LevelCollectionTableViewDidSelectLevelDelegate!, _cancellationToken);
        }

        private async void HandleDidSelectLevel(LevelCollectionViewController levelCollectionViewController, BeatmapLevel beatmapLevel)
        {
            Plugin.Log.Trace("LevelCollectionViewController.didSelectLevelEvent");

            Assert.That(beatmapLevel == _beatmapLevelCache.BeatmapLevel);

            await InvokeEventAsync(levelCollectionViewController, beatmapLevel, _eventProxyHooks.LevelCollectionViewControllerDidSelectLevelDelegate!, _cancellationToken);
        }

        private async void HandleDidChangeContent(StandardLevelDetailViewController? standardLevelDetailViewController, StandardLevelDetailViewController.ContentType contentType)
        {
            Plugin.Log.Trace($"StandardLevelDetailViewController.didChangeContentEvent {contentType}");

            if (standardLevelDetailViewController != null)
            {
                // The loaded level is a separate object, and is null while the preview is loading.
                Assert.That(standardLevelDetailViewController._previewBeatmapLevel == _beatmapLevelCache.BeatmapLevel);
            }

            await InvokeEventAsync(standardLevelDetailViewController, contentType, _eventProxyHooks.StandardLevelDetailViewControllerDidChangeContentDelegate!, _cancellationToken);
        }

#if LEGATO_BEFORE_MULTIPLAYER
        private void ReplaceDidSelectLevelEvent(Action<LevelCollectionViewController, bool, HMUI.ViewController.ActivationType> original, LevelCollectionViewController instance, bool firstActivation, HMUI.ViewController.ActivationType activationType)
#else
        private void ReplaceDidSelectLevelEvent(Action<LevelCollectionViewController, bool, bool, bool> original, LevelCollectionViewController instance, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
#endif
        {
            if (firstActivation)
            {
                Accessors.ViewControllerDidSelectLevelEventAccessor(ref instance) = HandleDidSelectLevel;
                Accessors.TableViewDidSelectLevelEventAccessor(ref instance._levelCollectionTableView) = HandleDidSelectLevel;
            }
#if LEGATO_BEFORE_MULTIPLAYER
            original(instance, firstActivation, activationType);
#else
            original(instance, firstActivation, addedToHierarchy, screenSystemEnabling);
#endif
        }

#if LEGATO_BEFORE_MULTIPLAYER
        private void ReplaceDidChangeContentEvent(Action<StandardLevelDetailViewController, bool, HMUI.ViewController.ActivationType> original, StandardLevelDetailViewController instance, bool firstActivation, HMUI.ViewController.ActivationType activationType)
#else
        private void ReplaceDidChangeContentEvent(Action<StandardLevelDetailViewController, bool, bool, bool> original, StandardLevelDetailViewController instance, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
#endif
        {
            if (firstActivation)
            {
                Accessors.DidChangeContentEventAccessor(ref instance) = HandleDidChangeContent;
            }
#if LEGATO_BEFORE_MULTIPLAYER
            original(instance, firstActivation, activationType);
#else
            original(instance, firstActivation, addedToHierarchy, screenSystemEnabling);
#endif
        }

        private void SetBeatmapLevelCacheBeatmapKey(Action<StandardLevelDetailView> original, StandardLevelDetailView instance)
        {
            original(instance);
            if (_beatmapLevelCache.BeatmapLevelDataLoadingTask != null)
                SetBeatmapLevelCacheBeatmapKeyAsync(instance.selectedDifficultyBeatmap);
        }

        private async void SetBeatmapLevelCacheBeatmapKeyAsync(BeatmapKey beatmapKey)
        {
            Plugin.Log.Debug("Attempting to set beatmap level cache beatmap key");

            var tcs = _beatmapLevelCache.BeatmapKeyTaskCompletionSource!;
            var loading = _beatmapLevelCache.BeatmapLevelDataLoadingTask!;
            var beatmapLevelData = await loading;
            if (!ReferenceEquals(loading, _beatmapLevelCache.BeatmapLevelDataLoadingTask) || !ReferenceEquals(tcs, _beatmapLevelCache.BeatmapKeyTaskCompletionSource)) return;

            // Task errored, faulted or canceled.
            if (beatmapLevelData == null)
            {
                tcs.TrySetResult(false);
                return;
            }

            Assert.That(beatmapLevelData == _beatmapLevelCache.BeatmapLevelData);

            if (!_beatmapLevelCache.DifficultyMatches(beatmapKey))
            {
                Plugin.Log.Debug($"Setting beatmap level cache beatmap key to {beatmapKey}");

                _beatmapLevelCache.InvalidateDifficulty();
                _beatmapLevelCache.BeatmapKey = beatmapKey;
            }

            tcs.TrySetResult(true);
        }

    }
}
#endif
