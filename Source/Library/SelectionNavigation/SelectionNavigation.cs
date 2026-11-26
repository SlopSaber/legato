using System;

namespace Legato.UI {
    internal static class SelectionNavigation {
        internal static BeatmapLevel Level(LevelSelectionNavigationController view) =>
#if LEGATO_BEFORE_MULTIPLAYER
            view._levelCollectionViewController._levelCollectionTableView._selectedPreviewBeatmapLevel;
#elif BEAT_SABER_1_29_0
            view.selectedBeatmapLevel;
#else
            view.beatmapLevel;
#endif
        internal static BeatmapKey Key(LevelSelectionNavigationController view) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static IDisposable SubscribeDifficulty(LevelSelectionNavigationController view, Action<LevelSelectionNavigationController> callback) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            Action<LevelSelectionNavigationController, IDifficultyBeatmap> handler = (sender, _) => callback(sender);
#else
            Action<LevelSelectionNavigationController> handler = callback;
#endif
            view.didChangeDifficultyBeatmapEvent += handler;
            return new Subscription(() => view.didChangeDifficultyBeatmapEvent -= handler);
        }
        internal static IDisposable SubscribePack(AnnotatedBeatmapLevelCollectionsViewController view, Action<NativeSongPack> callback) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            Action<IAnnotatedBeatmapLevelCollection> handler = collection => callback(collection as IBeatmapLevelPack);
#else
            Action<BeatmapLevelPack> handler = callback;
#endif
            view.didSelectAnnotatedBeatmapLevelCollectionEvent += handler;
            return new Subscription(() => view.didSelectAnnotatedBeatmapLevelCollectionEvent -= handler);
        }
        private sealed class Subscription : IDisposable {
            private Action _unsubscribe;
            internal Subscription(Action unsubscribe) { _unsubscribe = unsubscribe; }
            public void Dispose() { var unsubscribe = _unsubscribe; _unsubscribe = null; unsubscribe?.Invoke(); }
        }
    }
}
