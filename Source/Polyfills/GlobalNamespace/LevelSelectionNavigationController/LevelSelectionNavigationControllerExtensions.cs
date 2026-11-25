#nullable enable

namespace Legato {
    internal static class LevelSelectionNavigationControllerExtensions {
        internal static BeatmapKey GetBeatmapKey(this LevelSelectionNavigationController controller) {
#if LEGATO_DIFFICULTY_BEATMAPS
            return new BeatmapKey(controller.selectedDifficultyBeatmap);
#else
            return controller.beatmapKey;
#endif
        }

        internal static BeatmapKey? GetSelectedBeatmapKey(this LevelSelectionNavigationController controller) {
#if LEGATO_DIFFICULTY_BEATMAPS
            return controller.selectedDifficultyBeatmap == null ? null : new BeatmapKey(controller.selectedDifficultyBeatmap);
#else
            return controller.beatmapLevel == null ? null : controller.GetBeatmapKey();
#endif
        }

        internal static System.IDisposable SubscribeToDifficultyChanges(this LevelSelectionNavigationController controller, System.Action<BeatmapKey?> callback) =>
            new DifficultySubscription(controller, callback);

        private sealed class DifficultySubscription : System.IDisposable {
            private readonly LevelSelectionNavigationController _controller;
#if LEGATO_DIFFICULTY_BEATMAPS
            private readonly System.Action<LevelSelectionNavigationController, IDifficultyBeatmap> _handler;
#else
            private readonly System.Action<LevelSelectionNavigationController> _handler;
#endif
            internal DifficultySubscription(LevelSelectionNavigationController controller, System.Action<BeatmapKey?> callback) {
                _controller = controller;
#if LEGATO_DIFFICULTY_BEATMAPS
                _handler = (_, beatmap) => callback(beatmap == null ? null : new BeatmapKey(beatmap));
#else
                _handler = selection => callback(selection.GetSelectedBeatmapKey());
#endif
                _controller.didChangeDifficultyBeatmapEvent += _handler;
            }

            public void Dispose() => _controller.didChangeDifficultyBeatmapEvent -= _handler;
        }
    }
}
