#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
global using BeatmapLevelsEntitlementModel = global::AdditionalContentModel;
global using BeatSaberInit = global::AppInit;
global using EntitlementStatus = global::AdditionalContentModel.EntitlementStatus;
global using IPlayerDataModel = global::PlayerDataModel;
global using ViewControllerBase = global::HMUI.ViewController;
#endif

#if !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
#if LEGATO_BEFORE_MULTIPLAYER
global using LevelScenesTransitionSetupData = global::ScenesTransitionSetupDataSO;
#else
global using LevelScenesTransitionSetupData = global::LevelScenesTransitionSetupDataSO;
global using MultiplayerLevelScenesTransitionSetupData = global::MultiplayerLevelScenesTransitionSetupDataSO;
#endif
global using MissionLevelScenesTransitionSetupData = global::MissionLevelScenesTransitionSetupDataSO;
global using ScenesTransitionSetupData = global::ScenesTransitionSetupDataSO;
global using StandardLevelScenesTransitionSetupData = global::StandardLevelScenesTransitionSetupDataSO;
global using TutorialScenesTransitionSetupData = global::TutorialScenesTransitionSetupDataSO;
#endif
