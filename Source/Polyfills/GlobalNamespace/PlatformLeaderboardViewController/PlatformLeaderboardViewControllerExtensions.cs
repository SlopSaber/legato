#nullable enable

namespace Legato {
    internal static class PlatformLeaderboardViewControllerExtensions {
        internal static void SetData(this PlatformLeaderboardViewController controller, in BeatmapKey beatmapKey) {
#if LEGATO_DIFFICULTY_BEATMAPS
            controller.SetData(beatmapKey.difficultyBeatmap);
#else
            controller.SetData(beatmapKey);
#endif
        }
    }
}
