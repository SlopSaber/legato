using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Legato.UI {
    internal static class CollectionNavigation {
        internal const LevelPackDetailViewController.ContentType NonBuyable =
#if BEAT_SABER_1_29_0
            LevelPackDetailViewController.ContentType.Owned;
#else
            LevelPackDetailViewController.ContentType.NonBuyable;
#endif
        internal static IReadOnlyList<BeatmapLevel> Levels(LevelCollectionTableView table) =>
#if BEAT_SABER_1_29_0
            table._previewBeatmapLevels;
#else
            table._beatmapLevels;
#endif
        internal static NativeSongPack[] Packs(AnnotatedBeatmapLevelCollectionsViewController view) => view._annotatedBeatmapLevelCollections.OfType<NativeSongPack>().ToArray();
        internal static string CollectionId(NativeSongCollection collection) =>
#if BEAT_SABER_1_29_0
            (collection as IBeatmapLevelPack)?.packID;
#else
            collection.packID;
#endif
        internal static string CollectionName(NativeSongCollection collection) =>
#if BEAT_SABER_1_29_0
            collection.collectionName;
#else
            collection.packName;
#endif
        internal static Vector2 CellSize(GridView grid) =>
#if BEAT_SABER_1_29_0
            new Vector2(grid._dataSource.GetCellWidth(), grid._dataSource.GetCellHeight());
#else
            new Vector2(grid._dataSource.cellWidth, grid._dataSource.cellHeight);
#endif
        internal static Sprite DefaultCover(LevelPackDetailViewController view, Sprite fallback) =>
#if BEAT_SABER_1_29_0
            fallback;
#else
            view._defaultCoverSprite;
#endif
        internal static void SetPack(LevelCollectionNavigationController view, NativeSongPack pack, bool practice, bool favorites, string action, bool select) {
#if BEAT_SABER_1_29_0
            view.SetDataForPack(pack, practice, favorites, action);
#else
            view.SetDataForPack(pack, practice, favorites, action, select);
#endif
        }
        internal static BeatmapKey DetailKey(StandardLevelDetailViewController view) =>
#if BEAT_SABER_1_29_0
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static string CharacteristicName(BeatmapCharacteristic characteristic) =>
#if BEAT_SABER_1_29_0
            characteristic.serializedName;
#else
            characteristic.SerializedName();
#endif
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
