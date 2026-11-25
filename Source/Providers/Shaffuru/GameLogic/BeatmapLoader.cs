using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shaffuru.GameLogic {
	class BeatmapLoader {
		readonly GameplayCoreSceneSetupData _sceneSetupData;
		static BeatmapLevelsModel beatmapLevelsModel;

#if !BEAT_SABER_1_29_0
		readonly BeatmapLevelLoader beatmapLevelLoader;
		readonly BeatmapDataLoader beatmapDataLoader;
		readonly AudioClipAsyncLoader audioClipAsyncLoader;
#if !LEGATO_MAIN_SETTINGS_HANDLER
		readonly NativeAudioSettings settingsManager;
#endif
#endif
		static bool reloadLevelpacksOnExit = false;

		public BeatmapLoader(
			GameplayCoreSceneSetupData _sceneSetupData,
			BeatmapLevelsModel beatmapLevelsModel
#if !BEAT_SABER_1_29_0
			, BeatmapLevelLoader beatmapLevelLoader,
			BeatmapDataLoader beatmapDataLoader,
			AudioClipAsyncLoader audioClipAsyncLoader
#if !LEGATO_MAIN_SETTINGS_HANDLER
			, NativeAudioSettings settingsManager
#endif
#endif
		) {
			this._sceneSetupData = _sceneSetupData;
			BeatmapLoader.beatmapLevelsModel = beatmapLevelsModel;

#if !BEAT_SABER_1_29_0
			this.beatmapLevelLoader = beatmapLevelLoader;
			this.beatmapDataLoader = beatmapDataLoader;
			this.audioClipAsyncLoader = audioClipAsyncLoader;
#if !LEGATO_MAIN_SETTINGS_HANDLER
			this.settingsManager = settingsManager;
#endif
#endif
		}

		public static void AddBeatmapToLoadedPreviewBeatmaps(string levelId, BeatmapLevel level) {
#if BEAT_SABER_1_29_0
            beatmapLevelsModel._loadedPreviewBeatmapLevels[levelId] = level;
#elif BEAT_SABER_1_37_1 && !LEGATO_ADDITIONAL_PACK_LEVELS
            beatmapLevelsModel._allLoadedBeatmapLevelsRepository._idToBeatmapLevel[levelId] = level;
#else
            beatmapLevelsModel._allLoadedBeatmapLevelsRepository.AddBeatmapLevel(level, SongCore.Loader.CustomLevelsPack.packID);
#endif

			reloadLevelpacksOnExit = true;
		}

		public static BeatmapLevel GetPreviewBeatmapFromLevelId(string levelId) {
#if BEAT_SABER_1_29_0
            if (beatmapLevelsModel._loadedPreviewBeatmapLevels.TryGetValue(levelId, out var map))
#else
            if(beatmapLevelsModel._allLoadedBeatmapLevelsRepository.TryGetBeatmapLevelById(levelId, out var map))
#endif
				return map;

			return null;
		}

		public static bool RefreshLevelPacksIfNecessary() {
			if(!reloadLevelpacksOnExit)
				return false;

			reloadLevelpacksOnExit = false;

			SongCore.Loader.Instance.RefreshSongs(false);

			return true;
		}




#if BEAT_SABER_1_29_0
        public async Task<BeatmapLevel> LoadBeatmapLevel(string levelId) {
            var loaded = await beatmapLevelsModel.GetBeatmapLevelAsync(levelId, CancellationToken.None);
            return loaded.isError ? null : loaded.beatmapLevel;
        }
#else
        public Task<BeatmapLevel> LoadBeatmapLevel(string levelId) =>
            Task.FromResult(GetPreviewBeatmapFromLevelId(levelId));
#endif

		// Loads and transforms a difficulty the same way the game does when starting a level, so that
		// left-handed mode, environment filtering and wall merging apply to every queued map.
		public async Task<(IReadonlyBeatmapData beatmapData, UnityEngine.AudioClip audioClip)> LoadAndTransformBeatmapData(BeatmapLevel level, BeatmapKey key) {
#if BEAT_SABER_1_29_0
            // Native difficulty loading retains custom JSON and SongCore's v4 translation hooks.
            var settings = _sceneSetupData.playerSpecificSettings;
            var preset = key.difficulty == BeatmapDifficulty.ExpertPlus
                ? settings.environmentEffectsFilterExpertPlusPreset : settings.environmentEffectsFilterDefaultPreset;
            var data = await key.GetBeatmapDataAsync(_sceneSetupData.environmentInfo, settings);
            var transformed = BeatmapDataTransformHelper.CreateTransformedBeatmapData(data, level,
                _sceneSetupData.gameplayModifiers, settings.leftHanded, preset,
                _sceneSetupData.environmentInfo.environmentIntensityReductionOptions, _sceneSetupData.mainSettingsModel);
            return (transformed, ((IBeatmapLevel)level).beatmapLevelData.audioClip);
#else
			var loadResult = await beatmapLevelLoader.LoadBeatmapLevelDataAsync(level, BeatmapLevelDataVersion.Original, CancellationToken.None);
			var beatmapLevelData = loadResult.beatmapLevelData;

			if(beatmapLevelData == null) {
				Plugin.Log.Error($"Could not load beatmap data for {level.levelID}");
				return (null, null);
			}

#if LEGATO_TRANSFORMED_BEATMAP_LOADER
			var transformedBeatmapData = await beatmapDataLoader.CreateOrGetTransformedBeatmapDataAsync(
				key,
				level,
				_sceneSetupData.gameplayModifiers,
				_sceneSetupData.playerSpecificSettings,
				_sceneSetupData.targetEnvironmentInfo,
				_sceneSetupData.originalEnvironmentInfo,
				false,
				settingsManager.settings.quality.screenDisplacementEffects,
				beatmapLevelData,
				BeatmapLevelDataVersion.Original
			);
#else
			var playerSettings = _sceneSetupData.playerSpecificSettings;
			var modifiers = _sceneSetupData.gameplayModifiers;
#if LEGATO_SINGLE_BEATMAP_ENVIRONMENT
			var environment = _sceneSetupData.environmentInfo;
#else
			var environment = _sceneSetupData.targetEnvironmentInfo;
#endif
			var data = await beatmapDataLoader.LoadBeatmapDataAsync(
				beatmapLevelData, key, level.beatsPerMinute,
				level.GetEnvironmentName(key.beatmapCharacteristic, key.difficulty) == (EnvironmentName)environment.serializedName,
				environment,
#if !LEGATO_SINGLE_BEATMAP_ENVIRONMENT
				_sceneSetupData.originalEnvironmentInfo,
#endif
				BeatmapLevelDataVersion.Original, modifiers, playerSettings, enableBeatmapDataCaching: false);
			var preset = key.difficulty == BeatmapDifficulty.ExpertPlus
				? playerSettings.environmentEffectsFilterExpertPlusPreset : playerSettings.environmentEffectsFilterDefaultPreset;
			var transformedBeatmapData = BeatmapDataTransformHelper.CreateTransformedBeatmapData(
				data, level, modifiers, playerSettings.leftHanded, preset,
#if LEGATO_SINGLE_BEATMAP_ENVIRONMENT
				environment.environmentIntensityReductionOptions, _sceneSetupData._performancePreset);
#else
				_sceneSetupData.originalEnvironmentInfo.environmentIntensityReductionOptions, in settingsManager.settings);
#endif
#endif

			var audioClip = await audioClipAsyncLoader.LoadSong(beatmapLevelData);

			return (transformedBeatmapData, audioClip);
#endif
		}

#if BEAT_SABER_1_29_0
        public static IDifficultyBeatmap GetDifficultyBeatmapData(BeatmapLevel level, BeatmapKey key) => key;
#else
		public static BeatmapBasicData GetDifficultyBeatmapData(BeatmapLevel level, BeatmapKey key) =>
			Legato.BeatmapKeyExtensions.GetDifficultyBeatmapData(level, in key);
#endif
	}
}
