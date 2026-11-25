#if LEGATO_HECK_LEGACY_GAMEPLAY
using System.Runtime.CompilerServices;
using Heck.Settings;
using SiraUtil.Affinity;

namespace Heck.ReLoad;

internal sealed class ReLoadRestart : IAffinity
{
    private readonly Config.ReLoaderSettings _config;
    private readonly ReLoaderLoader _loader;
    private readonly ConditionalWeakTable<StandardLevelScenesTransitionSetupData, IDifficultyBeatmap> _practiceMaps = new();

    private ReLoadRestart(ReLoaderLoader loader, Config.ReLoaderSettings config)
    {
        _loader = loader;
        _config = config;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(StandardLevelScenesTransitionSetupData), nameof(StandardLevelScenesTransitionSetupData.Init))]
    private void CapturePracticeMap(StandardLevelScenesTransitionSetupData __instance, IDifficultyBeatmap difficultyBeatmap, PracticeSettings? practiceSettings)
    {
        _practiceMaps.Remove(__instance);
        if (practiceSettings != null)
        {
            _practiceMaps.Add(__instance, difficultyBeatmap);
        }
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(MenuTransitionsHelper), "HandleMainGameSceneDidFinish")]
    private void ReloadOnRestart(StandardLevelScenesTransitionSetupData standardLevelScenesTransitionSetupData, LevelCompletionResults levelCompletionResults)
    {
        if (_config.ReloadOnRestart && levelCompletionResults.levelEndAction == LevelCompletionResults.LevelEndAction.Restart &&
            _practiceMaps.TryGetValue(standardLevelScenesTransitionSetupData, out IDifficultyBeatmap map))
        {
            _loader.Reload(map);
        }
    }
}
#endif
