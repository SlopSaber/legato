#nullable enable

namespace Legato {
    internal static class LevelCollectionNavigationControllerExtensions {
        internal static void SelectLevel(this LevelCollectionNavigationController controller, BeatmapLevel level) {
#if LEGATO_DIFFICULTY_BEATMAPS
            controller.SelectLevel(level.previewBeatmapLevel);
#else
            controller.SelectLevel(level);
#endif
        }
    }
}
