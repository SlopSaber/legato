using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if !BEAT_SABER_1_29_0
using BeatmapEditor3D;
#endif
using HarmonyLib;
using Shaffuru.GameLogic;
using SiraUtil.Zenject;
using UnityEngine;
using Zenject;

namespace Shaffuru.MenuLogic {
	class Anlasser {
		public const string LevelIdPrefix = "___Shaffuru_";

		readonly PlayerDataModel playerDataModel;
		readonly MenuTransitionsHelper menuTransitionsHelper;
		readonly EnvironmentsListModel environmentsListModel;

		static int lastShaffuruMapLength = 0;

		public static BeatmapLevel beatmapLevel { get; private set; }
		public static BeatmapKey beatmapKey { get; private set; }
#if BEAT_SABER_1_29_0
        static readonly BeatmapDataSO beatmapLevelData = ScriptableObject.CreateInstance<BeatmapDataSO>();
#else
        static BeatmapEditor3D.BeatmapLevelData beatmapLevelData;
#endif
#if LEGATO_SCRIPTABLE_CHARACTERISTICS
        readonly BeatmapCharacteristicCollectionSO characteristics;
#endif
		static AudioClip fakeAudioClip;

		static UBinder<Plugin, System.Random> rngSource;

#if BEAT_SABER_1_29_0
        static EnvironmentInfoSO defaultEnvironment;
#else
        static EnvironmentName defaultEnvironment;
#endif

		public Anlasser(
			PlayerDataModel playerDataModel,
			CustomLevelLoader customLevelLoader,
			[InjectOptional] MenuTransitionsHelper menuTransitionsHelper,
			[InjectOptional] EnvironmentsListModel environmentsListModel,
#if LEGATO_SCRIPTABLE_CHARACTERISTICS
            BeatmapCharacteristicCollectionSO characteristics,
#endif
            UBinder<Plugin, System.Random> rng
		) {
			this.playerDataModel = playerDataModel;
			this.menuTransitionsHelper = menuTransitionsHelper;
			this.environmentsListModel = environmentsListModel;

			rngSource = rng;

#if LEGATO_SCRIPTABLE_CHARACTERISTICS
            this.characteristics = characteristics;
#endif
#if BEAT_SABER_1_29_0
            defaultEnvironment = customLevelLoader.LoadEnvironmentInfo("", false);
#else
            defaultEnvironment = customLevelLoader.CreateEnvironmentName("", customLevelLoader._defaultEnvironmentInfo);
#endif
		}

		public event Action<LevelCompletionResults> finishedOrFailedCallback;

#if !BEAT_SABER_1_29_0
		// Bare-bones IPreviewMediaData for our fake level - we don't have (or need) a real preview clip
		class FakePreviewMediaData : IPreviewMediaData {
#if LEGATO_CANCELLABLE_COVER_LOADING
			public Task<Sprite> GetCoverSpriteAsync(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested
				? Task.FromCanceled<Sprite>(cancellationToken) : Task.FromResult(SongCore.Loader.defaultCoverImage);
			public Task<AudioClip> GetPreviewAudioClip(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested
				? Task.FromCanceled<AudioClip>(cancellationToken) : Task.FromResult(fakeAudioClip);
#else
			public Task<Sprite> GetCoverSpriteAsync() => Task.FromResult(SongCore.Loader.defaultCoverImage);
			public Task<AudioClip> GetPreviewAudioClip() => Task.FromResult(fakeAudioClip);
#endif
			public void UnloadPreviewAudioClip() { }
			public void UnloadCoverSprite() { }
		}

		static readonly FakePreviewMediaData previewMediaData = new FakePreviewMediaData();

#endif

		public void UpdateFakeBeatmap(int lengthSeconds) {
			if(lengthSeconds > 480 * 60)
				throw new ArgumentOutOfRangeException();

			const float bpm = 13.37f;
			const BeatmapDifficulty difficulty = BeatmapDifficulty.ExpertPlus;

			var levelId = $"{LevelIdPrefix}{lengthSeconds}";

			if(lastShaffuruMapLength != lengthSeconds) {
				if(fakeAudioClip != null)
					GameObject.Destroy(fakeAudioClip);

				fakeAudioClip = AudioClip.Create("", lengthSeconds * 1000, 1, 1000, false);

				lastShaffuruMapLength = lengthSeconds;
			}

			var comedy = (DateTime.Now.Day == 1 && DateTime.Now.Month == 4) || rngSource.Value.NextDouble() >= 0.98;
			var a = (bpm * ((lengthSeconds - 1) / 60f)).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

			string beatmapJson;

			if(comedy) {
				beatmapJson = @"{""version"":""3.0.0"",""basicBeatmapEvents"":[{""i"":3,""f"":3}],""colorNotes"":[{""b"":0.33,""x"":1,""y"":1,""d"":8},{""b"":" + a + @",""x"":1,""y"":1,""d"":8}],""bombNotes"":[],""obstacles"":[{""b"":0.33,""y"":2,""d"":0.02,""w"":1,""h"":-3},{""b"":0.33,""x"":2,""y"":2,""d"":0.02,""w"":1,""h"":-3},{""b"":0.33,""y"":2,""d"":0.02,""w"":3,""h"":1},{""b"":0.33,""x"":1,""y"":0,""d"":0.02,""w"":1,""h"":1},{""b"":0.33,""x"":3,""d"":0.02,""w"":1,""h"":2},{""b"":" + a + @",""x"":0,""d"":0.03,""w"":4,""h"":5}],""bpmEvents"":[],""rotationEvents"":[],""sliders"":[],""burstSliders"":[],""waypoints"":[],""colorBoostBeatmapEvents"":[],""lightColorEventBoxGroups"":[],""lightRotationEventBoxGroups"":[],""basicEventTypesWithKeywords"":[],""useNormalEventsAsCompatibleEvents"":true}";
			} else {
				beatmapJson = @"{""version"":""3.0.0"",""basicBeatmapEvents"":[{""b"":0,""et"":3,""i"":0,""f"":3}],""colorNotes"":[{""b"":0.5,""x"":1,""y"":0,""c"":1,""d"":1},{""b"":0.5,""x"":1,""y"":1,""c"":1,""d"":1},{""b"":0.5,""x"":1,""y"":2,""c"":1,""d"":1},{""b"":0.5,""x"":2,""y"":0,""c"":0,""d"":8},{""b"":" + a + @",""x"":1,""y"":1,""c"":0,""d"":8}],""bombNotes"":[],""obstacles"":[{""b"":0.4,""x"":3,""y"":0,""d"":3,""w"":1,""h"":5},{""b"":0.4,""x"":0,""y"":0,""d"":3,""w"":1,""h"":5},{""b"":" + a + @",""x"":0,""y"":0,""d"":0.1,""w"":4,""h"":5}],""bpmEvents"":[],""rotationEvents"":[],""sliders"":[],""burstSliders"":[],""waypoints"":[],""colorBoostBeatmapEvents"":[],""lightColorEventBoxGroups"":[],""lightRotationEventBoxGroups"":[],""basicEventTypesWithKeywords"":[],""useNormalEventsAsCompatibleEvents"":true}";
			}

#if BEAT_SABER_1_29_0
            beatmapLevelData.SetJsonData(beatmapJson);
            var nativeLevel = beatmapLevel as BeatmapLevelSO ?? ScriptableObject.CreateInstance<BeatmapLevelSO>();
            var nativeKey = new BeatmapLevelSO.DifficultyBeatmap(nativeLevel, difficulty, 0, 10, 0, beatmapLevelData);
            var standard = characteristics.GetBeatmapCharacteristicBySerializedName("Standard");
            nativeLevel.InitFull(levelId, $"Shaffuru ({lengthSeconds / 60} Minutes)", "", "Kinsi55", "Kinsi55",
                fakeAudioClip, bpm, 0, 0, 0, 0, 0, SongCore.Loader.defaultCoverImage,
                defaultEnvironment, defaultEnvironment,
                new[] { new BeatmapLevelSO.DifficultyBeatmapSet(standard, new[] { nativeKey }) });
            beatmapLevel = nativeLevel;
            beatmapKey = nativeKey;
#else
			var audioJson = @"{""version"":""4.0.0"",""songChecksum"":"""",""songSampleCount"":0,""songFrequency"":0,""bpmData"":[],""lufsData"":[]}";
			var lightshowJson = @"{""version"":""4.0.0"",""waypoints"":[],""basicEvents"":[],""colorBoostEvents"":[],""eventBoxGroups"":[]}";

			beatmapLevelData = new BeatmapEditor3D.BeatmapLevelData(
				"Shaffuru",
				fakeAudioClip,
				audioJson,
				beatmapJson,
				lightshowJson
			);

			var beatmapBasicData = new BeatmapBasicData(
				noteJumpMovementSpeed: 10,
				noteJumpStartBeatOffset: 0,
				environmentName: defaultEnvironment,
				beatmapColorScheme: null,
				notesCount: comedy ? 2 : 5,
#if !LEGATO_LEGACY_BEATMAP_NOTE_COUNT
				cuttableObjectsCount: comedy ? 2 : 5,
#endif
				obstaclesCount: comedy ? 6 : 3,
				bombsCount: 0,
				mappers: new[] { "Kinsi55" },
				lighters: new[] { "Kinsi55" }
			);

#if LEGATO_SCRIPTABLE_CHARACTERISTICS
			var standard = characteristics.allBeatmapCharacteristics.First(characteristic => characteristic.serializedName == "Standard");
#else
			var standard = BeatmapCharacteristic.Standard;
#endif
#if BEAT_SABER_1_45_3
			var beatmapBasicDatas = new Dictionary<(GameplayType, BeatmapCharacteristic, BeatmapDifficulty), BeatmapBasicData> {
				[(GameplayType.BeatSaber, standard, difficulty)] = beatmapBasicData
			};
#else
			var beatmapBasicDatas = new Dictionary<(BeatmapCharacteristic, BeatmapDifficulty), BeatmapBasicData> {
				[(standard, difficulty)] = beatmapBasicData
			};
#endif

			beatmapLevel = new BeatmapLevel(
				version: 1,
				hasPrecalculatedData: false,
				levelID: levelId,
				songName: $"Shaffuru ({lengthSeconds / 60} Minutes)",
				songSubName: "",
				songAuthorName: "Kinsi55",
				allMappers: new[] { "Kinsi55" },
				allLighters: new[] { "Kinsi55" },
				beatsPerMinute: bpm,
				integratedLufs: 0,
				songTimeOffset: 0,
				previewStartTime: 0,
				previewDuration: 0,
				songDuration: lengthSeconds,
				contentRating: default,
				previewMediaData: previewMediaData,
				beatmapBasicData: beatmapBasicDatas
			);

#if BEAT_SABER_1_45_3
			beatmapKey = new BeatmapKey(levelId, GameplayType.BeatSaber, standard, difficulty);
#else
			beatmapKey = new BeatmapKey(levelId, standard, difficulty);
#endif
#endif
		}

		public void Start(int lengthSeconds, int rngSeed = 0) {
			if(rngSeed != 0)
				rngSource.Value = new System.Random(rngSeed);

			UpdateFakeBeatmap(lengthSeconds);

			var endCallback = new Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>((a, b) => {
				if(b.levelEndAction == LevelCompletionResults.LevelEndAction.Restart) {
					Start(lengthSeconds);
					return;
				}

				BeatmapLoader.RefreshLevelPacksIfNecessary();

				if(b.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared ||
					b.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed ||
					// If user is dum dum and plays with nofail and then backs out to menu we show this too because we are nice :)
					b.energy == 0f
				) {
					finishedOrFailedCallback?.Invoke(b);
				}
			});

			var key = beatmapKey;

			var colorSchemesSettings = playerDataModel.playerData.colorSchemesSettings;

#if BEAT_SABER_1_29_0
            menuTransitionsHelper.StartStandardLevel("Shaffuru", key, beatmapLevel,
                playerDataModel.playerData.overrideEnvironmentSettings, colorSchemesSettings.GetOverrideColorScheme(),
                playerDataModel.playerData.gameplayModifiers, playerDataModel.playerData.playerSpecificSettings,
                null, "Exit", false, false, null, endCallback, null);
#elif LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
			menuTransitionsHelper.StartStandardLevel(
				Legato.GameModes.Solo, in key, beatmapLevel, beatmapLevelData,
				playerDataModel.playerData.overrideEnvironmentSettings, colorSchemesSettings.GetOverrideColorScheme(),
#if !LEGATO_SINGLE_COLOR_OVERRIDE
				colorSchemesSettings.ShouldOverrideLightshowColors(),
#endif
				null, playerDataModel.playerData.gameplayModifiers, playerDataModel.playerData.playerSpecificSettings,
				null, environmentsListModel, "Menu", false, false, null, null, endCallback, null);
#else
			menuTransitionsHelper.StartStandardLevel(
				Legato.GameModes.Solo,
				in key,
				beatmapLevel,
				playerDataModel.playerData.overrideEnvironmentSettings,
				colorSchemesSettings.GetOverrideColorScheme(),
				colorSchemesSettings.ShouldOverrideLightshowColors(),
				playerDataModel.playerData.gameplayModifiers,
				playerDataModel.playerData.playerSpecificSettings,
				null,
				environmentsListModel,
				new GameplayAdditionalInformation("Menu"),
				null,
				null,
				endCallback,
				null,
				beatmapLevelData
			);
#endif
		}
	}
}
