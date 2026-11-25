#if PRE_V1_37_1
using Heck.Module;
using SiraUtil.Affinity;
#if LEGATO_HECK_LEGACY_GAMEPLAY
using MissionLevelScenesTransitionSetupData = MissionLevelScenesTransitionSetupDataSO;
using StandardLevelScenesTransitionSetupData = StandardLevelScenesTransitionSetupDataSO;
using TutorialScenesTransitionSetupData = TutorialScenesTransitionSetupDataSO;
#endif

namespace Heck.HarmonyPatches.ModuleActivator;

internal class SceneTransitionModuleActivator : IAffinity
{
    private readonly ModuleManager _moduleManager;

    internal SceneTransitionModuleActivator(ModuleManager moduleManager)
    {
        _moduleManager = moduleManager;
    }

    [AffinityPrefix]
    [AffinityPatch(
        typeof(MissionLevelScenesTransitionSetupData),
        nameof(MissionLevelScenesTransitionSetupData.Init))]
#if LEGATO_HECK_LEGACY_GAMEPLAY
    private void MissionPrefix(IDifficultyBeatmap difficultyBeatmap, ref OverrideEnvironmentSettings? overrideEnvironmentSettings)
#else
    private void MissionPrefix(IDifficultyBeatmap difficultyBeatmap)
#endif
    {
#if !LEGATO_HECK_LEGACY_GAMEPLAY
        OverrideEnvironmentSettings? overrideEnvironmentSettings = null;
#endif
        _moduleManager.Activate(difficultyBeatmap, LevelType.Mission, ref overrideEnvironmentSettings);
    }

#if !LEGATO_HECK_LEGACY_GAMEPLAY
    [AffinityPrefix]
    [AffinityPatch(
        typeof(MultiplayerLevelScenesTransitionSetupData),
        nameof(MultiplayerLevelScenesTransitionSetupData.Init))]
    private void MultiplayerPrefix(IDifficultyBeatmap difficultyBeatmap)
    {
        OverrideEnvironmentSettings? overrideEnvironmentSettings = null;
        _moduleManager.Activate(difficultyBeatmap, LevelType.Multiplayer, ref overrideEnvironmentSettings);
    }
#endif

    [AffinityPrefix]
    [AffinityPatch(
        typeof(StandardLevelScenesTransitionSetupData),
        nameof(StandardLevelScenesTransitionSetupData.Init))]
    private void StandardPrefix(
        IDifficultyBeatmap difficultyBeatmap,
        ref OverrideEnvironmentSettings? overrideEnvironmentSettings)
    {
        _moduleManager.Activate(difficultyBeatmap, LevelType.Standard, ref overrideEnvironmentSettings);
    }

    [AffinityPrefix]
    [AffinityPatch(
        typeof(TutorialScenesTransitionSetupData),
        nameof(TutorialScenesTransitionSetupData.Init))]
    private void TutorialPrefix()
    {
        OverrideEnvironmentSettings? overrideEnvironmentSettings = null;
        _moduleManager.Activate(null, LevelType.Standard, ref overrideEnvironmentSettings);
    }
}
#endif
