#nullable enable

namespace Legato {
    internal static class LevelCollectionViewControllerExtensions {
        internal static void SelectLevel(this LevelCollectionViewController controller, BeatmapLevel level) {
#if LEGATO_DIFFICULTY_BEATMAPS
            controller.SelectLevel(level.previewBeatmapLevel);
#else
            controller.SelectLevel(level);
#endif
        }
    }
}
