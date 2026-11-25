#nullable enable

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
#if LEGATO_LEGACY_PREVIEW
            transition.Get<GameplayCoreSceneSetupData>()?.difficultyBeatmap;
#else
            transition.difficultyBeatmap;
#endif
#else
        internal static BeatmapLevel? GetBeatmapLevel(this StandardLevelScenesTransitionSetupData transition) => transition.beatmapLevel;

        internal static BeatmapKey GetBeatmapKey(this StandardLevelScenesTransitionSetupData transition) => transition.beatmapKey;
#endif
    }
}
