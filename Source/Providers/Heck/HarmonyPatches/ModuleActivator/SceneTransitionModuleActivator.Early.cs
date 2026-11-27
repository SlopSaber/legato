#if LEGATO_EARLY_STANDARD_MAPS
using System;
using Heck.Module;
using SiraUtil.Affinity;

namespace Heck.HarmonyPatches.ModuleActivator;

internal sealed class SceneTransitionModuleActivator : IAffinity
{
    private readonly ModuleManager _modules;
    private SceneInfo? _overrideEnvironment;

    private SceneTransitionModuleActivator(ModuleManager modules)
    {
        _modules = modules;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(MainGameSceneSetupData), nameof(MainGameSceneSetupData.Init))]
    private void ActivateForLevel(IStandardLevelDifficultyBeatmap difficultyLevel)
    {
        _overrideEnvironment = null;
        _modules.Activate(difficultyLevel, LevelType.Standard, ref _overrideEnvironment);
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(MenuSceneSetupData), nameof(MenuSceneSetupData.StartTutorial))]
    private void DeactivateForTutorial()
    {
        _overrideEnvironment = null;
        _modules.Activate(null, LevelType.Standard, ref _overrideEnvironment);
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(AsyncScenesLoader), nameof(AsyncScenesLoader.LoadScenes))]
    private void SelectStandardEnvironment(ref SceneInfo[] scenes)
    {
        if (_overrideEnvironment == null || Array.FindIndex(scenes, scene => scene.sceneName == "StandardLevel") <= 0)
        {
            return;
        }

        // The native loader places the gameplay environment before StandardLevel and GameCore.
        scenes = (SceneInfo[])scenes.Clone();
        scenes[0] = _overrideEnvironment;
    }
}
#endif
