#if LEGATO_BEFORE_MULTIPLAYER
global using Legato.Runtime;
global using NativeBeatmapCharacteristic = global::BeatmapCharacteristicSO;
#elif BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
global using HapticPresetSO = BeatSaber.Haptics.HapticPresetSO;
global using StandardLevelScenesTransitionSetupDataSO = StandardLevelScenesTransitionSetupData;
global using TutorialScenesTransitionSetupDataSO = TutorialScenesTransitionSetupData;
global using MissionLevelScenesTransitionSetupDataSO = MissionLevelScenesTransitionSetupData;
global using MultiplayerLevelScenesTransitionSetupDataSO = MultiplayerLevelScenesTransitionSetupData;
global using NativeBeatmapCharacteristic = global::BeatmapCharacteristic;
global using NativeHapticFeedbackManager = BeatSaber.Haptics.HapticFeedbackManager;
#elif BEAT_SABER_1_29_0
global using HapticPresetSO = Libraries.HM.HMLib.VR.HapticPresetSO;
global using NativeBeatmapCharacteristic = global::BeatmapCharacteristicSO;
global using NativeHapticFeedbackManager = global::HapticFeedbackController;
#else
global using HapticPresetSO = Libraries.HM.HMLib.VR.HapticPresetSO;
global using NativeBeatmapCharacteristic = global::BeatmapCharacteristicSO;
global using NativeHapticFeedbackManager = global::HapticFeedbackManager;
#endif

#if LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
global using NativeBeatmapKey = global::IDifficultyBeatmap;
#else
global using NativeBeatmapKey = global::BeatmapKey;
#endif
