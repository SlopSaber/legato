#nullable enable

namespace Legato {
    internal static partial class GameplayCoreSceneSetupDataExtensions {
        internal static BeatmapKey GetBeatmapKey(this GameplayCoreSceneSetupData instance) =>
#if LEGATO_DIFFICULTY_BEATMAPS
            new BeatmapKey(instance.difficultyBeatmap);
#else
            instance.beatmapKey;
#endif

        internal static BeatmapLevel? GetBeatmapLevel(this GameplayCoreSceneSetupData instance) =>
#if LEGATO_HISTORICAL_GAME
            instance.difficultyBeatmap?.level is IBeatmapLevel level ? new BeatmapLevel(level) : null;
#elif LEGATO_DIFFICULTY_BEATMAPS
            instance.previewBeatmapLevel == null ? null : new BeatmapLevel(instance.previewBeatmapLevel);
#else
            instance.beatmapLevel;
#endif
    }
}
