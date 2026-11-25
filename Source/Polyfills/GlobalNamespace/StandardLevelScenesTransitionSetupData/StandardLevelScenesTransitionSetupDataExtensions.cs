#nullable enable

#if LEGATO_DIFFICULTY_IN_SCENE_SETUP
using System.Linq;
#endif

#if !LEGATO_NO_TRANSITION_DIFFICULTY
namespace Legato {
    internal static class StandardLevelScenesTransitionSetupDataExtensions {
#if LEGATO_DIFFICULTY_BEATMAPS
        internal static BeatmapLevel? GetBeatmapLevel(this StandardLevelScenesTransitionSetupData transition) {
            var level = transition.GetDifficultyBeatmap()?.level;
            return level == null ? null : new BeatmapLevel(level);
        }

        internal static BeatmapKey GetBeatmapKey(this StandardLevelScenesTransitionSetupData transition) =>
            new BeatmapKey(transition.GetDifficultyBeatmap());

        private static IDifficultyBeatmap? GetDifficultyBeatmap(this StandardLevelScenesTransitionSetupData transition) =>
#if LEGATO_DIFFICULTY_IN_SCENE_SETUP
            transition.sceneSetupDataArray?.OfType<GameplayCoreSceneSetupData>().FirstOrDefault()?.difficultyBeatmap;
#else
            transition.difficultyBeatmap;
#endif
#else
        internal static BeatmapLevel? GetBeatmapLevel(this StandardLevelScenesTransitionSetupData transition) => transition.beatmapLevel;

        internal static BeatmapKey GetBeatmapKey(this StandardLevelScenesTransitionSetupData transition) => transition.beatmapKey;
#endif
    }
}
#endif
