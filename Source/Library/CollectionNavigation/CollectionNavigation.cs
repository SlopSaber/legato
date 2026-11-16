using System;
using System.Collections.Generic;
using UnityEngine;

namespace Legato.UI {
    internal static class CollectionNavigation {
        internal const string RowSelectionMethod =
#if BEAT_SABER_1_29_0
            "HandleDidSelectRowEvent";
#else
            "HandleDidSelectCellWithIndex";
#endif
        internal static NativeSongPack Selected(AnnotatedBeatmapLevelCollectionsViewController view) =>
#if BEAT_SABER_1_29_0
            view.selectedAnnotatedBeatmapLevelCollection as IBeatmapLevelPack;
#else
            view.selectedAnnotatedBeatmapLevelPack;
#endif
        internal static IReadOnlyList<NativeSongPack> CustomPacks(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0
            model.customLevelPackCollection.beatmapLevelPacks;
#else
            model._customLevelsRepository.beatmapLevelPacks;
#endif
        internal static BeatmapLevel Level(LevelCollectionNavigationController view) =>
#if BEAT_SABER_1_29_0
            view.selectedBeatmapLevel;
#else
            view.beatmapLevel;
#endif
        internal static NativeSongCollection Collection(AnnotatedBeatmapLevelCollectionCell cell) =>
#if BEAT_SABER_1_29_0
            cell._annotatedBeatmapLevelCollection;
#else
            cell._beatmapLevelPack;
#endif
        internal static IDisposable Subscribe(LevelFilteringNavigationController view, Action<NativeSongPack> callback) {
#if BEAT_SABER_1_29_0
            Action<LevelFilteringNavigationController, IAnnotatedBeatmapLevelCollection, GameObject, BeatmapCharacteristicSO> handler = (_, pack, info, characteristic) => callback(pack as IBeatmapLevelPack);
            view.didSelectAnnotatedBeatmapLevelCollectionEvent += handler;
            return new Subscription(() => view.didSelectAnnotatedBeatmapLevelCollectionEvent -= handler);
#else
            Action<LevelFilteringNavigationController, BeatmapLevelPack, GameObject, LevelSelectionOptions> handler = (_, pack, info, options) => callback(pack);
            view.didSelectBeatmapLevelPackEvent += handler;
            return new Subscription(() => view.didSelectBeatmapLevelPackEvent -= handler);
#endif
        }
        private sealed class Subscription : IDisposable {
            private Action _unsubscribe;
            internal Subscription(Action unsubscribe) { _unsubscribe = unsubscribe; }
            public void Dispose() { var callback = _unsubscribe; _unsubscribe = null; callback?.Invoke(); }
        }
    }
}
