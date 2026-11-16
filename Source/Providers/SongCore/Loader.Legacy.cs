#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Settings;
using IPA.Utilities;
using IPA.Utilities.Async;
using Legato.Beatmaps.Formats;
using Newtonsoft.Json;
using SongCore.Data;
using SongCore.UI;
using SongCore.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace SongCore
{
    public partial class Loader
    {
        private Loader(GameScenesManager scenes, LevelFilteringNavigationController filtering, LevelPackDetailViewController packs,
            BeatmapLevelsModel levels, CustomLevelLoader loader, ProgressBar progress, PluginConfig config,
            SettingsController settings, BSMLSettings bsml)
        {
            _gameScenesManager = scenes; _levelFilteringNavigationController = filtering; _levelPackDetailViewController = packs;
            _beatmapLevelsModel = levels; _customLevelLoader = loader; _progressBar = progress; _config = config;
            _settingsController = settings; _bsmlSettings = bsml;
            _spriteAsyncLoader = loader._cachedMediaAsyncLoader;
            _beatmapCharacteristicCollection = loader._beatmapCharacteristicCollection;
            _customWIPPath = Path.GetFullPath(Path.Combine(Application.dataPath, "CustomWIPLevels"));
            _customLevelsPath = Path.GetFullPath(CustomLevelPathHelper.customLevelsDirectoryPath);
            Instance = this;
        }

        public void Initialize()
        {
            _gameScenesManager.transitionDidFinishEvent += HandleSceneTransitionDidFinish;
            Legato.Presentation.BsmlSettingsMenu.Add(_bsmlSettings, nameof(SongCore), "SongCore.UI.settings.bsml", _settingsController);
            SongsLoadedEvent += HandleSongsLoaded;
        }

        public void Dispose()
        {
            Legato.Presentation.BsmlSettingsMenu.Remove(_bsmlSettings, _settingsController);
            _gameScenesManager.transitionDidFinishEvent -= HandleSceneTransitionDidFinish;
            _gameScenesManager.transitionDidStartEvent -= HandleSceneTransitionDidStart;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SongsLoadedEvent -= HandleSongsLoaded;
        }

        private async void HandleSceneTransitionDidFinish(ScenesTransitionSetupDataSO setup, DiContainer container)
        {
            _gameScenesManager.transitionDidFinishEvent -= HandleSceneTransitionDidFinish;
            BeatmapLevelsModelSO = _beatmapLevelsModel; CustomLevelLoader = _customLevelLoader;
            cachedMediaAsyncLoaderSO = _spriteAsyncLoader; beatmapCharacteristicCollection = _beatmapCharacteristicCollection;
            defaultCoverImage = _customLevelLoader._defaultPackCover;
            if (Hashing.cachedSongHashData.IsEmpty)
            {
                await Task.WhenAll(Hashing.LoadCachedSongHashesAsync(), Hashing.LoadCachedAudioDataAsync());
                RefreshSongs();
            }
            else RefreshLevelPacks();
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            _gameScenesManager.transitionDidStartEvent += HandleSceneTransitionDidStart;
        }

        private void HandleSceneTransitionDidStart(float duration) => CancelSongLoading();

        private void RefreshNativeLevelPacks()
        {
            _beatmapLevelsModel.ClearLoadedBeatmapLevelsCaches();
            _beatmapLevelsModel._customLevelPackCollection = CustomLevelsRepository;
            _beatmapLevelsModel.UpdateAllLoadedBeatmapLevelPacks();
            _beatmapLevelsModel.UpdateLoadedPreviewLevels();
        }

        private BeatmapLevel CreateNativePreview(NativeLoadedSaveData data)
        {
            StandardLevelInfoSaveData info = data.standardLevelInfoSaveData
                ?? StandardLevelInfoSaveData.DeserializeFromJSONString(data.beatmapLevelSaveData!.LegacyMetadata(true).ToString(Formatting.None));
            var sets = info.difficultyBeatmapSets.Select(set => new PreviewDifficultyBeatmapSet(
                _beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(set.beatmapCharacteristicName),
                set.difficultyBeatmaps.Select(difficulty => Enum.TryParse(difficulty.difficulty, out BeatmapDifficulty value)
                    ? value : throw new InvalidDataException("Unknown beatmap difficulty: " + difficulty.difficulty)).ToArray())).ToArray();
            return new CustomPreviewBeatmapLevel(defaultCoverImage, info, data.customLevelFolderInfo.folderPath,
                _spriteAsyncLoader, string.Empty, info.songName, info.songSubName, info.songAuthorName, info.levelAuthorName,
                info.beatsPerMinute, info.songTimeOffset, info.shuffle, info.shufflePeriod, info.previewStartTime, info.previewDuration,
                _customLevelLoader.LoadEnvironmentInfo(info.environmentName, false),
                _customLevelLoader.LoadEnvironmentInfo(info.allDirectionsEnvironmentName, true), sets);
        }

        private static void SetNativeLevelId(BeatmapLevel level, string id) =>
            ((CustomPreviewBeatmapLevel)level).SetField("<levelID>k__BackingField", id);

        private static void BlacklistLevelFiles(NativeLoadedSaveData data)
        {
            // Legacy native data exposes parsed difficulties rather than the file-backed JSON API.
        }

        private (string hash, BeatmapLevel beatmapLevel)? LoadCustomLevelInternal(string path, SongFolderEntry? entry = null)
        {
            string filename = Path.Combine(path, CustomLevelPathHelper.kStandardLevelInfoFilename);
            if (!File.Exists(filename)) return null;
            var document = V4Documents.Read(filename);
            var folder = new CustomLevelFolderInfo(Path.GetFullPath(path), new DirectoryInfo(path).Name, document.ToString(Formatting.None));
            NativeLoadedSaveData data = new() { customLevelFolderInfo = folder };
            if ((document["version"] ?? document["_version"])?.Value<string>() is string version && Version.TryParse(version, out Version? parsed) && parsed.Major >= 4)
            {
                var v4 = new V4LevelInfo(document);
                if (!File.Exists(v4.ResolveAsset(path, v4.AudioFilename))) return null;
                data.beatmapLevelSaveData = v4;
            }
            else
            {
                var info = StandardLevelInfoSaveData.DeserializeFromJSONString(folder.levelInfoJsonString);
                if (info == null || info.difficultyBeatmapSets.Length == 0 || !File.Exists(Path.Combine(path, info.songFilename))) return null;
                data.standardLevelInfoSaveData = info;
            }
            return LoadSong(data, entry);
        }

        public static BeatmapLevel? GetLevelById(string id) => string.IsNullOrEmpty(id) ? null : BeatmapLevelsModelSO.GetLevelPreviewForLevelId(id);

        public static BeatmapLevel? GetLevelByHash(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return null;
            string id = Collections.levelIDsForHash(hash).FirstOrDefault() ?? CustomLevelLoader.kCustomLevelPrefixId + hash.ToUpperInvariant();
            return GetLevelById(id);
        }
    }
}
#endif
