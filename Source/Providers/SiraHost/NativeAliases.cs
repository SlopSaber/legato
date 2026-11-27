#if LEGATO_SIRA_EARLY_CONTEXTS
global using FlowCoordinator = VRUI.VRUIFlowCoordinator;
global using ViewController = VRUI.VRUIViewController;
global using SaberType = global::Saber.SaberType;
#else
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
global using LocalizationAsset = Polyglot.LocalizationAsset;
#else
global using LocalizationAsset = BGLib.Polyglot.LocalizationAsset;
#endif

#if LEGATO_PC_APP_INIT
global using BeatSaberInit = global::PCAppInit;
#endif

#if LEGATO_BEFORE_MULTIPLAYER
global using BeatmapObjectsInstaller = global::GameplayCoreBeatmapObjectPoolsInstaller;
global using EffectPoolsManualInstaller = global::EffectPoolsInstaller;
global using GameplayCoreInstaller = global::GameplayCoreSceneSetup;
global using MissionGameplayInstaller = global::MissionGameplaySceneSetup;
global using SaberModelController = global::BasicSaberModelController;
global using StandardGameplayInstaller = global::StandardGameplaySceneSetup;
global using TutorialInstaller = global::TutorialSceneSetup;
#endif
#endif
