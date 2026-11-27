#if LEGATO_BSUTILS_EARLY
global using BeatmapLevel = global::IStandardLevel;
global using SaberType = global::Saber.SaberType;
global using GameplayCoreSceneSetupData = global::MainGameSceneSetupData;
global using LevelScenesTransitionSetupData = global::GameSceneSetupData;
global using ScenesTransitionSetupData = global::GameSceneSetupData;
global using StandardLevelScenesTransitionSetupData = global::MainGameSceneSetupData;
global using TutorialScenesTransitionSetupData = global::TutorialSceneSetupData;
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
global using BeatmapLevel = global::IPreviewBeatmapLevel;
global using NativeBeatmapLevelPack = global::IBeatmapLevelPack;
#else
global using NativeBeatmapLevelPack = global::BeatmapLevelPack;
#endif
