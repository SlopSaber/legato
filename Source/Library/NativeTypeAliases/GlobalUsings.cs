#if BEAT_SABER_1_29_0
global using BeatSaberInit = global::AppInit;
global using LocalizationAsset = Polyglot.LocalizationAsset;
global using StandardLevelScenesTransitionSetupData = global::StandardLevelScenesTransitionSetupDataSO;
global using MissionLevelScenesTransitionSetupData = global::MissionLevelScenesTransitionSetupDataSO;
global using MultiplayerLevelScenesTransitionSetupData = global::MultiplayerLevelScenesTransitionSetupDataSO;
#else
global using LocalizationAsset = BGLib.Polyglot.LocalizationAsset;
#endif
