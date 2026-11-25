#nullable enable

namespace Legato {
    internal static class MultiplayerLevelScenesTransitionSetupDataExtensions {
#if LEGATO_DIFFICULTY_BEATMAPS
        internal static BeatmapLevel? GetBeatmapLevel(this MultiplayerLevelScenesTransitionSetupData transition) =>
            transition.previewBeatmapLevel == null ? null : new BeatmapLevel(transition.previewBeatmapLevel);

        internal static BeatmapKey GetBeatmapKey(this MultiplayerLevelScenesTransitionSetupData transition) =>
            new BeatmapKey(transition.difficultyBeatmap);
#else
        internal static BeatmapLevel? GetBeatmapLevel(this MultiplayerLevelScenesTransitionSetupData transition) => transition.beatmapLevel;

        internal static BeatmapKey GetBeatmapKey(this MultiplayerLevelScenesTransitionSetupData transition) => transition.beatmapKey;
#endif
    }
}
