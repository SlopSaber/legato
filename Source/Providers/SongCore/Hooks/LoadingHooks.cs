using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using Zenject;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using NativePackCollection = global::IBeatmapLevelPackCollection;
#else
using NativePackCollection = global::BeatmapLevelsRepository;
#endif

namespace SongCore.Hooks
{
    internal class LoadingHooks : IInitializable, IDisposable
    {
        private Hook _reloadCustomLevelPackCollectionAsyncHook = null!;
#if !LEGATO_BEFORE_MULTIPLAYER
        private Hook _updateCustomSongsHook = null!;
#endif

        public void Initialize()
        {
#if LEGATO_BEFORE_MULTIPLAYER
            _reloadCustomLevelPackCollectionAsyncHook = new Hook(typeof(BeatmapLevelsModel).GetMethod(nameof(BeatmapLevelsModel.GetCustomLevelPackCollectionAsync))!, SuppressReload, true);
#else
            _reloadCustomLevelPackCollectionAsyncHook = new Hook(typeof(BeatmapLevelsModel).GetMethod(nameof(BeatmapLevelsModel.ReloadCustomLevelPackCollectionAsync))!, SuppressReload, true);
            _updateCustomSongsHook = new Hook(typeof(LevelFilteringNavigationController).GetMethod(nameof(LevelFilteringNavigationController.UpdateCustomSongs), BindingFlags.Instance | BindingFlags.NonPublic)!, SetPacksAndRefreshUI, true);
#endif
        }

        public void Dispose()
        {
            _reloadCustomLevelPackCollectionAsyncHook.Dispose();
#if !LEGATO_BEFORE_MULTIPLAYER
            _updateCustomSongsHook.Dispose();
#endif
        }

        // TODO: Some code in LoadBeatmapLevelCollectionAsync is skipped that is important.
        private Task<NativePackCollection?> SuppressReload(Func<BeatmapLevelsModel, CancellationToken, Task<NativePackCollection?>> original, BeatmapLevelsModel instance, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if LEGATO_BEFORE_MULTIPLAYER
            return Task.FromResult<NativePackCollection?>(Loader.CustomLevelsRepository ?? SongCore.OverrideClasses.SongCoreBeatmapLevelsRepository.CreateNew());
#elif BEAT_SABER_1_29_0
            return Task.FromResult<NativePackCollection?>(Loader.CustomLevelsRepository);
#else
            return Task.FromResult<NativePackCollection?>(null);
#endif
        }

#if !LEGATO_BEFORE_MULTIPLAYER
        private void SetPacksAndRefreshUI(Action<LevelFilteringNavigationController> original, LevelFilteringNavigationController instance)
        {
            if (Loader.CustomLevelsRepository == null)
            {
                return;
            }

            instance._customLevelPacks = Loader.CustomLevelsRepository.beatmapLevelPacks;
            IEnumerable<NativeSongPack>? packs = null;
            if (instance._ostBeatmapLevelPacks != null)
            {
                packs = instance._ostBeatmapLevelPacks;
            }

            if (instance._musicPacksBeatmapLevelPacks != null)
            {
                packs = packs == null ? instance._musicPacksBeatmapLevelPacks : packs.Concat(instance._musicPacksBeatmapLevelPacks);
            }

            if (instance._customLevelPacks != null)
            {
                packs = packs == null ? instance._customLevelPacks : packs.Concat(instance._customLevelPacks);
            }

            instance._allBeatmapLevelPacks = packs.ToArray();
            instance._levelSearchViewController.Setup(instance._allBeatmapLevelPacks);
            instance.UpdateSecondChildControllerContent(instance._selectLevelCategoryViewController.selectedLevelCategory);
        }
#endif
    }
}
