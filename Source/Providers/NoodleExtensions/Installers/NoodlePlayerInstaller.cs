using JetBrains.Annotations;
using NoodleExtensions.Animation;
using NoodleExtensions.HarmonyPatches.FakeNotes;
#if !LEGATO_NOODLE_LEGACY_GAMEPLAY
using NoodleExtensions.HarmonyPatches.Mirror;
#endif
using NoodleExtensions.HarmonyPatches.ObjectProcessing;
using NoodleExtensions.HarmonyPatches.Objects;
using NoodleExtensions.HarmonyPatches.SmallFixes;
using NoodleExtensions.Managers;
using Zenject;

namespace NoodleExtensions.Installers;

[UsedImplicitly]
internal class NoodlePlayerInstaller : Installer
{
    private readonly FeaturesModule _featuresModule;

    private NoodlePlayerInstaller(FeaturesModule featuresModule)
    {
        _featuresModule = featuresModule;
    }

    public override void InstallBindings()
    {
        if (!_featuresModule.Active)
        {
            return;
        }

#if !LEGATO_NOODLE_LEGACY_GAMEPLAY
        Container.Bind<SpawnDataManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<NoodleObjectsCallbacksManager>().AsSingle();
#endif
        Container.Bind<CutoutManager>().AsSingle();
        Container.Bind<AnimationHelper>().AsSingle();
        Container.BindInterfacesAndSelfTo<NoodlePlayerTransformManager>().AsSingle();

        // Custom Events
        Container.BindInterfacesTo<AssignTrackParent>().AsSingle();
        Container.BindInterfacesTo<AssignPlayerToTrack>().AsSingle();

        // Cutout
        Container.BindInterfacesAndSelfTo<ObjectInitializer>().AsSingle();

        // FakeNotes
        Container.BindInterfacesTo<FakeNotePatches>().AsSingle();
#if !LEGATO_NOODLE_LEGACY_GAMEPLAY
        Container.BindInterfacesTo<MiscFakePatches>().AsSingle();
#endif
        Container.BindInterfacesAndSelfTo<FakePatchesManager>().AsSingle();

#if !LEGATO_NOODLE_LEGACY_GAMEPLAY
        // Mirror
        Container.BindInterfacesTo<MirroredNoteNoodleTracker>().AsSingle();
        Container.BindInterfacesTo<MirroredObstacleNoodleTracker>().AsSingle();
#endif

        // ObjectProcessing
        Container.BindInterfacesAndSelfTo<ManagedActiveObstacleTracker>().AsSingle();
#if LEGATO_NOODLE_LEGACY_GAMEPLAY
        Container.BindInterfacesTo<LegacySpawnMovement>().AsSingle();
        Container.BindInterfacesTo<LegacySpawnScheduler>().AsSingle();
#else
        Container.BindInterfacesTo<NoodledSpawnMovementData>().AsSingle();
        Container.BindInterfacesTo<ObjectCallbackAheadTimeReorder>().AsSingle();
#endif

#if !PRE_V1_40_8
        // VariableMovementProvider
        Container
            .BindMemoryPool<NoodleMovementDataProvider, NoodleMovementDataProvider.Pool>()
            .WithInitialSize(40);
        Container.BindInterfacesTo<ReplaceMovementDataProvider>().AsSingle();
#endif

        // Objects
        Container.BindInterfacesTo<GameNoteCutNoodlifier>().AsSingle();
        Container.BindInterfacesTo<NoteFloorMovementNoodlifier>().AsSingle();
        Container.BindInterfacesTo<NoteInitNoodlifier>().AsSingle();
        Container.BindInterfacesAndSelfTo<NoteJumpNoodlifier>().AsSingle();
        Container.BindInterfacesAndSelfTo<NoteUpdateNoodlifier>().AsSingle();
        Container.BindInterfacesTo<ObstacleInitNoodlifier>().AsSingle();
        Container.BindInterfacesTo<ObstacleUpdateNoodlifier>().AsSingle();
#if !LEGATO_NOODLE_LEGACY_GAMEPLAY
        Container.BindInterfacesTo<SliderInitNoodlifier>().AsSingle();
        Container.BindInterfacesTo<SliderUpdateNoodlifier>().AsSingle();
#endif
        Container.BindInterfacesTo<NoteLinker>().AsSingle();

        // SmallFixes
        Container.BindInterfacesTo<PreventObstacleFlickerOnSpawn>().AsSingle();
#if !LEGATO_NOODLE_LEGACY_GAMEPLAY
        Container.Bind<InitializedSpawnMovementData>().AsSingle().NonLazy();
#endif
        Container.BindInterfacesTo<PlayerTransformsNoodlePatch>().AsSingle();
        Container.BindInterfacesTo<SaberPlayerMovementFix>().AsSingle();
    }
}
