#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatmapDataLoaderVersion4;
using BeatmapLevelSaveDataVersion4;
using BeatmapSaveDataVersion4;
using BeatSaberMarkupLanguage.Settings;
using JsonFileHandler = Legato.Json.NativeJsonFileHandler;
using JsonSettings = Legato.Json.NativeJsonSettings;
using IPA.Utilities;
using IPA.Utilities.Async;
using Newtonsoft.Json;
using SongCore.Data;
using SongCore.Hooks.BeatmapLevelCache;
using SongCore.OverrideClasses;
using SongCore.UI;
using SongCore.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
#if LEGATO_SCRIPTABLE_SCENE_TRANSITIONS
using SceneTransitionSetup = global::ScenesTransitionSetupDataSO;
#else
using SceneTransitionSetup = global::ScenesTransitionSetupData;
#endif

namespace SongCore
{
    public partial class Loader
    {
        private Loader(GameScenesManager gameScenesManager, LevelFilteringNavigationController levelFilteringNavigationController, LevelPackDetailViewController levelPackDetailViewController, BeatmapLevelsModel beatmapLevelsModel, CustomLevelLoader customLevelLoader, SpriteAsyncLoader spriteAsyncLoader, BeatmapCharacteristicCollection beatmapCharacteristicCollection, ProgressBar progressBar, PluginConfig config, SettingsController settingsController, BSMLSettings bsmlSettings)

        {
            _gameScenesManager = gameScenesManager;
            _levelFilteringNavigationController = levelFilteringNavigationController;
            _levelPackDetailViewController = levelPackDetailViewController;
            _beatmapLevelsModel = beatmapLevelsModel;
            _customLevelLoader = customLevelLoader;
            _spriteAsyncLoader = spriteAsyncLoader;
            _beatmapCharacteristicCollection = beatmapCharacteristicCollection;
            _progressBar = progressBar;
            _bsmlSettings = bsmlSettings;
            _config = config;
            _settingsController = settingsController;
            // Path.GetFullPath is needed to normalize directory separators.
            _customWIPPath = Path.GetFullPath(Path.Combine(Application.dataPath, "CustomWIPLevels"));
            _customLevelsPath = Path.GetFullPath(CustomLevelPathHelper.customLevelsDirectoryPath);
            Instance = this;
        }

        public void Initialize()
        {
            _gameScenesManager.transitionDidFinishEvent += HandleSceneTransitionDidFinish;
            // BSML might fail to find the resource if done in a patched method.
            Legato.Presentation.BsmlSettingsMenu.Add(_bsmlSettings, nameof(SongCore), "SongCore.UI.settings.bsml", _settingsController);

            SongsLoadedEvent += HandleSongsLoaded;
        }

        public void Dispose()
        {
            Legato.Presentation.BsmlSettingsMenu.Remove(_bsmlSettings, _settingsController);

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            _gameScenesManager.transitionDidStartEvent -= HandleSceneTransitionDidStart;

            SongsLoadedEvent -= HandleSongsLoaded;
        }

        private async void HandleSceneTransitionDidFinish(
#if !LEGATO_UNTYPED_SCENE_TRANSITIONS
            GameScenesManager.SceneTransitionType sceneTransitionType,
#endif
            SceneTransitionSetup scenesTransitionSetupData, DiContainer container)
        {
            _gameScenesManager.transitionDidFinishEvent -= HandleSceneTransitionDidFinish;

            // Ensures that the static references are still valid Unity objects.
            // They'll be destroyed on internal restart.
            BeatmapLevelsModelSO = _beatmapLevelsModel;
            CustomLevelLoader = _customLevelLoader;
            cachedMediaAsyncLoaderSO = _spriteAsyncLoader;
            defaultCoverImage = _levelPackDetailViewController._defaultCoverSprite;
            beatmapCharacteristicCollection = _beatmapCharacteristicCollection;

            if (Hashing.cachedSongHashData.IsEmpty)
            {
                await Task.WhenAll(Hashing.LoadCachedSongHashesAsync(), Hashing.LoadCachedAudioDataAsync());
                RefreshSongs();
            }
            else
            {
                RefreshLevelPacks();
            }

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;

            _gameScenesManager.transitionDidStartEvent += HandleSceneTransitionDidStart;
        }

        private void HandleSceneTransitionDidStart(
#if !LEGATO_UNTYPED_SCENE_TRANSITIONS
            GameScenesManager.SceneTransitionType sceneTransitionType,
#endif
            float duration)
        {
            CancelSongLoading();
        }





        private static void BlacklistLevelFiles(NativeLoadedSaveData loadedSaveData)
        {
            const string reason = "Use IBeatmapLevelData to read level difficulty and audio data. Info.dat data can be found in CustomLevelLoader._loadedBeatmapSaveData";

            if (!string.IsNullOrEmpty(loadedSaveData.beatmapLevelSaveData?.audio.audioDataFilename))
            {
                IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, loadedSaveData.beatmapLevelSaveData!.audio.audioDataFilename)), reason);
            }

            foreach (var difficultyBeatmap in loadedSaveData.standardLevelInfoSaveData?.difficultyBeatmapSets.SelectMany(x => x.difficultyBeatmaps) ?? [])
            {
                if (!string.IsNullOrEmpty(difficultyBeatmap.beatmapFilename))
                {
                    IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapFilename)), reason);
                }
            }

            foreach (var difficultyBeatmap in loadedSaveData.beatmapLevelSaveData?.difficultyBeatmaps ?? [])
            {
                if (!string.IsNullOrEmpty(difficultyBeatmap.beatmapDataFilename))
                {
                    IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapDataFilename)), reason);
                }

                if (!string.IsNullOrEmpty(difficultyBeatmap.lightshowDataFilename))
                {
                    IOBlacklistHook.BlacklistedFiles.TryAdd(Path.GetFullPath(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, difficultyBeatmap.lightshowDataFilename)), reason);
                }
            }
        }

        private (string hash, BeatmapLevel beatmapLevel)? LoadCustomLevelInternal(string customLevelPath, SongFolderEntry? entry = null)
        {
            var infoFilePath = Path.Combine(customLevelPath, CustomLevelPathHelper.kStandardLevelInfoFilename);
            if (!File.Exists(infoFilePath))
            {
                return null;
            }

            NativeLoadedSaveData loadedSaveData;
            var directoryInfo = new DirectoryInfo(customLevelPath);
            string json;

            IOBlacklistHook.AllowIO.Value = true;
            try
            {
                json = File.ReadAllText(infoFilePath);
            }
            finally
            {
                IOBlacklistHook.AllowIO.Value = false;
            }

            var version = BeatmapSaveDataHelpers.GetVersion(json);
            if (version < BeatmapSaveDataHelpers.version4)
            {
                var standardLevelInfoSaveData = StandardLevelInfoSaveData.DeserializeFromJSONString(json);
                if (standardLevelInfoSaveData == null || standardLevelInfoSaveData.difficultyBeatmapSets.Length == 0)
                {
                    return null;
                }

                var customLevelFolderInfo = new CustomLevelFolderInfo(directoryInfo.FullName, directoryInfo.Name, json);
                if (!AudioExists(customLevelFolderInfo, standardLevelInfoSaveData.songFilename))
                {
                    return null;
                }

                loadedSaveData = new NativeLoadedSaveData { customLevelFolderInfo = customLevelFolderInfo, standardLevelInfoSaveData = standardLevelInfoSaveData };
            }
            else
            {
                var beatmapLevelSaveData = JsonConvert.DeserializeObject<BeatmapLevelSaveData>(json, JsonSettings.readableWithDefault);
                if (beatmapLevelSaveData == null || beatmapLevelSaveData.difficultyBeatmaps.Length == 0)
                {
                    return null;
                }

                var customLevelFolderInfo = new CustomLevelFolderInfo(directoryInfo.FullName, directoryInfo.Name, json);
                if (!AudioExists(customLevelFolderInfo, beatmapLevelSaveData.audio.songFilename))
                {
                    return null;
                }

#if !LEGATO_UNMIGRATED_V4_LEVEL_INFO
                BeatmapLevelSaveDataUtils.MigrateBeatmapLevelSaveData(beatmapLevelSaveData);
#endif
                loadedSaveData = new NativeLoadedSaveData { customLevelFolderInfo = customLevelFolderInfo, beatmapLevelSaveData = beatmapLevelSaveData };
            }

            return LoadSong(loadedSaveData, entry);
        }

        private static bool AudioExists(CustomLevelFolderInfo folder, string fileName) =>
#if LEGATO_INLINE_AUDIO_CHECK
            File.Exists(Path.Combine(folder.folderPath, fileName));
#else
            CustomLevelLoader.CheckIfAudioExists(folder, fileName);
#endif

        public static BeatmapLevel? GetLevelById(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            return BeatmapLevelsModelSO.GetBeatmapLevel(levelId);
        }

        public static BeatmapLevel? GetLevelByHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return null;
            }

#if LEGATO_CASE_SENSITIVE_LEVEL_LOOKUP
            return BeatmapLevelsModelSO.GetBeatmapLevel(CustomLevelLoader.kCustomLevelPrefixId + hash.ToUpperInvariant());
#else
            return BeatmapLevelsModelSO.GetBeatmapLevel(CustomLevelLoader.kCustomLevelPrefixId + hash, true);
#endif
        }

        private void GetSongDuration(NativeLoadedSaveData loadedSaveData, BeatmapLevel beatmapLevel)
        {
            try
            {
                var levelID = beatmapLevel.levelID;
                float length = 0;
                Hashing.TryGetRelativePath(loadedSaveData.customLevelFolderInfo.folderPath, out var relativePath);
                if (Hashing.cachedAudioData.TryGetValue(relativePath, out var data))
                {
                    if (data.id == levelID)
                    {
                        length = data.duration;
                    }
                }

                if (length == 0)
                {
                    IOBlacklistHook.AllowIO.Value = true;
                    try
                    {
                        if (loadedSaveData.standardLevelInfoSaveData != null)
                        {
                            length = GetLengthFromAudio(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, loadedSaveData.standardLevelInfoSaveData.songFilename));
                        }
                        else if (loadedSaveData.beatmapLevelSaveData != null)
                        {
                            length = GetLengthFromAudio(Path.Combine(loadedSaveData.customLevelFolderInfo.folderPath, loadedSaveData.beatmapLevelSaveData.audio.songFilename));
                        }
                    }
                    catch (Exception)
                    {
                        length = -1;
                    }
                    finally
                    {
                        IOBlacklistHook.AllowIO.Value = false;
                    }

                    if (length <= 1)
                    {
                        // janky, but whatever
                        Plugin.Log.Warn($"Failed to parse song length from audio file, approximating using map length. Song: {loadedSaveData.customLevelFolderInfo.folderPath}");

                        IBeatmapLevelData beatmapLevelData = null!;

                        if (loadedSaveData.standardLevelInfoSaveData != null)
                        {
                            beatmapLevelData = _customLevelLoader.CreateBeatmapLevelDataFromV3(loadedSaveData.customLevelFolderInfo, loadedSaveData.standardLevelInfoSaveData);
                        }
                        else if (loadedSaveData.beatmapLevelSaveData != null)
                        {
                            beatmapLevelData = _customLevelLoader.CreateBeatmapLevelDataFromV4(loadedSaveData.customLevelFolderInfo, loadedSaveData.beatmapLevelSaveData);
                        }

                        length = GetLengthFromMap(loadedSaveData, beatmapLevelData, beatmapLevel);
                    }
                }

                if (data != null)
                {
                    data.duration = length;
                    data.id = levelID;
                }
                else
                {
                    Hashing.cachedAudioData[relativePath] = new AudioCacheData(levelID, length);
                }

                if (_config.ForceLongPreviews)
                {
                    Accessors.PreviewDurationAccessor(ref beatmapLevel) = Mathf.Max(beatmapLevel.previewDuration, length - beatmapLevel.previewStartTime);
                }

                Accessors.SongDurationAccessor(ref beatmapLevel) = length;
            }
            catch (Exception ex)
            {
                Plugin.Log.Warn("Failed to parse song duration");
                Plugin.Log.Warn(ex);
            }
        }

        public static float GetLengthFromMap(NativeLoadedSaveData loadedSaveData, IBeatmapLevelData beatmapLevelData, BeatmapLevel level)
        {
            float length = 0;
            var fileSystemBeatmapLevelData = (FileSystemBeatmapLevelData)beatmapLevelData;

            if (loadedSaveData.standardLevelInfoSaveData != null)
            {
                var beatmapKey = level.GetBeatmapKeys().Last();
                string? json;

                IOBlacklistHook.AllowIO.Value = true;
                try
                {
                    json = OriginalMethods.FileSystemBeatmapLevelData.GetBeatmapString(fileSystemBeatmapLevelData, beatmapKey);
                }
                finally
                {
                    IOBlacklistHook.AllowIO.Value = false;
                }

                var beatmapSaveData = JsonUtility.FromJson<BeatmapSaveDataVersion3.BeatmapSaveData>(json);

                float highestTime = 0;
                if (beatmapSaveData.colorNotes.Count > 0)
                {
                    highestTime = beatmapSaveData.colorNotes.Max(x => x.beat);
                }
                else if (beatmapSaveData.basicBeatmapEvents.Count > 0)
                {
                    highestTime = beatmapSaveData.basicBeatmapEvents.Max(x => x.beat);
                }

                var bpmTimeProcessor = new BpmTimeProcessor(level.beatsPerMinute, beatmapSaveData.bpmEvents);
                length = bpmTimeProcessor.ConvertBeatToTime(highestTime);
            }
            else if (loadedSaveData.beatmapLevelSaveData != null)
            {
                var beatmapKey = level.GetBeatmapKeys().Last();
                string? beatmapJson;
                string? lightShowJson;
                string? audioJson;

                IOBlacklistHook.AllowIO.Value = true;
                try
                {
                    audioJson = OriginalMethods.FileSystemBeatmapLevelData.GetAudioDataString(fileSystemBeatmapLevelData);
                    beatmapJson = OriginalMethods.FileSystemBeatmapLevelData.GetBeatmapString(fileSystemBeatmapLevelData, beatmapKey);
                    lightShowJson = OriginalMethods.FileSystemBeatmapLevelData.GetLightshowString(fileSystemBeatmapLevelData, beatmapKey);
                }
                finally
                {
                    IOBlacklistHook.AllowIO.Value = false;
                }

                var beatmapSaveData = JsonUtility.FromJson<BeatmapSaveData>(beatmapJson);
                var lightshowSaveData = JsonUtility.FromJson<LightshowSaveData>(lightShowJson);
                var audioSaveData = JsonUtility.FromJson<AudioSaveData>(audioJson);

                float highestTime = 0;
                if (beatmapSaveData.colorNotes.Length > 0)
                {
                    highestTime = beatmapSaveData.colorNotes.Max(x => x.beat);
                }
                else if (lightshowSaveData.basicEvents.Length > 0)
                {
                    highestTime = lightshowSaveData.basicEvents.Max(x => x.beat);
                }

                var bpmTimeProcessor = new BpmTimeProcessor(audioSaveData);
                length = bpmTimeProcessor.ConvertBeatToTime(highestTime);
            }

            if (length == 0)
            {
                Plugin.Log.Warn($"Failed to parse song length from audio file. Song: {loadedSaveData.customLevelFolderInfo.folderPath}");
            }

            return length;
        }
        private BeatmapLevel CreateNativePreview(NativeLoadedSaveData data) => data.standardLevelInfoSaveData != null
            ? _customLevelLoader.CreateBeatmapLevelFromV3(data.customLevelFolderInfo, data.standardLevelInfoSaveData)
            : _customLevelLoader.CreateBeatmapLevelFromV4(data.customLevelFolderInfo, data.beatmapLevelSaveData!);

        private static void SetNativeLevelId(BeatmapLevel level, string id) => Accessors.LevelIDAccessor(ref level) = id;

        private void RefreshNativeLevelPacks()
        {
            _beatmapLevelsModel.ClearLoadedBeatmapLevelsCaches();
            _beatmapLevelsModel._customLevelsRepository = CustomLevelsRepository;
            _beatmapLevelsModel.LoadAllBeatmapLevelPacks();
        }
    }
}
#endif
