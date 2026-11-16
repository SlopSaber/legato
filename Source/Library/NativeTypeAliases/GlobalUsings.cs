#if BEAT_SABER_1_29_0
global using IPlayerDataModel = global::PlayerDataModel;
global using BeatmapLevelsEntitlementModel = global::AdditionalContentModel;
global using EntitlementStatus = global::AdditionalContentModel.EntitlementStatus;
global using BeatSaberInit = global::AppInit;
global using ViewControllerBase = global::HMUI.ViewController;
#endif

#if !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
global using LevelScenesTransitionSetupData = global::LevelScenesTransitionSetupDataSO;
global using ScenesTransitionSetupData = global::ScenesTransitionSetupDataSO;
global using TutorialScenesTransitionSetupData = global::TutorialScenesTransitionSetupDataSO;
global using StandardLevelScenesTransitionSetupData = global::StandardLevelScenesTransitionSetupDataSO;
global using MissionLevelScenesTransitionSetupData = global::MissionLevelScenesTransitionSetupDataSO;
global using MultiplayerLevelScenesTransitionSetupData = global::MultiplayerLevelScenesTransitionSetupDataSO;
#endif
