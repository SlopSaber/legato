#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
global using HapticPresetSO = BeatSaber.Haptics.HapticPresetSO;
global using StandardLevelScenesTransitionSetupDataSO = StandardLevelScenesTransitionSetupData;
global using TutorialScenesTransitionSetupDataSO = TutorialScenesTransitionSetupData;
global using MissionLevelScenesTransitionSetupDataSO = MissionLevelScenesTransitionSetupData;
global using MultiplayerLevelScenesTransitionSetupDataSO = MultiplayerLevelScenesTransitionSetupData;
global using NativeBeatmapCharacteristic = global::BeatmapCharacteristic;
global using NativeHapticFeedbackManager = BeatSaber.Haptics.HapticFeedbackManager;
#else
global using HapticPresetSO = Libraries.HM.HMLib.VR.HapticPresetSO;
global using NativeBeatmapCharacteristic = global::BeatmapCharacteristicSO;
global using NativeHapticFeedbackManager = global::HapticFeedbackManager;
#endif
