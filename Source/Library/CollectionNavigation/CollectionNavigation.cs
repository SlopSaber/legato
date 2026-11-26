using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if LEGATO_BEFORE_MULTIPLAYER
using CollectionController = LevelSelectionNavigationController;
using CollectionCell = AnnotatedBeatmapLevelCollectionTableCell;
#else
using CollectionController = LevelCollectionNavigationController;
using CollectionCell = AnnotatedBeatmapLevelCollectionCell;
#endif

namespace Legato.UI {
    internal static partial class CollectionNavigation {
        internal static string EmptyCustomPackId => CustomLevelLoader.kCustomLevelPackPrefixId + "CustomLevels";
        internal static string LevelId(BeatmapKey key) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            key?.level?.levelID ?? string.Empty;
#else
            key.levelId;
#endif
        internal static bool EmptySetData(object[] args) =>
#if LEGATO_BEFORE_MULTIPLAYER
            args[0] == null || ((IBeatmapLevelCollection)args[0]).beatmapLevels.Length == 0;
#elif BEAT_SABER_1_29_0
            args[0] == null || ((IBeatmapLevelCollection)args[0]).beatmapLevels.Count == 0;
#else
            args[0] == null || ((IReadOnlyList<BeatmapLevel>)args[0]).Count == 0;
#endif
        internal const LevelPackDetailViewController.ContentType NonBuyable =
#if LEGATO_PACK_CONTENT_WITHOUT_PURCHASE_OPTIONS
            LevelPackDetailViewController.ContentType.Owned;
#else
            LevelPackDetailViewController.ContentType.NonBuyable;
#endif
        internal static IReadOnlyList<BeatmapLevel> Levels(LevelCollectionTableView table) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            table._previewBeatmapLevels;
#else
            table._beatmapLevels;
#endif
        internal static NativeSongPack[] Packs(AnnotatedBeatmapLevelCollectionsViewController view) => view._annotatedBeatmapLevelCollections.OfType<NativeSongPack>().ToArray();
        internal static string CollectionId(NativeSongCollection collection) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            (collection as IBeatmapLevelPack)?.packID;
#else
            collection.packID;
#endif
        internal static string CollectionName(NativeSongCollection collection) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            collection.collectionName;
#else
            collection.packName;
#endif
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static Vector2 CellSize(GridView grid) =>
#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE
            new Vector2(grid._dataSource.GetCellWidth(), grid._dataSource.GetCellHeight());
#else
            new Vector2(grid._dataSource.cellWidth, grid._dataSource.cellHeight);
#endif
#endif
        internal static Sprite DefaultCover(LevelPackDetailViewController view, Sprite fallback) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            fallback;
#else
            view._defaultCoverSprite;
#endif
        internal static void SetPack(CollectionController view, NativeSongPack pack, bool practice, bool favorites, string action, bool select) {
#if LEGATO_BEFORE_MULTIPLAYER
            view.SetData(pack, true, true, practice, null);
#elif BEAT_SABER_1_29_0
            view.SetDataForPack(pack, practice, favorites, action);
#else
            view.SetDataForPack(pack, practice, favorites, action, select);
#endif
        }
        internal static BeatmapKey DetailKey(StandardLevelDetailViewController view) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static string CharacteristicName(BeatmapCharacteristic characteristic) =>
#if LEGATO_CHARACTERISTIC_NAME_PROPERTY || LEGATO_BEFORE_MULTIPLAYER
            characteristic.serializedName;
#else
            characteristic.SerializedName();
#endif
        internal static NativeSongPack Selected(AnnotatedBeatmapLevelCollectionsViewController view) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            view.selectedAnnotatedBeatmapLevelCollection as IBeatmapLevelPack;
#else
            view.selectedAnnotatedBeatmapLevelPack;
#endif
        internal static IReadOnlyList<NativeSongPack> CustomPacks(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            model.customLevelPackCollection.beatmapLevelPacks;
#else
            model._customLevelsRepository.beatmapLevelPacks;
#endif
        internal static BeatmapLevel Level(CollectionController view) =>
#if LEGATO_BEFORE_MULTIPLAYER
            view._levelCollectionViewController._levelCollectionTableView._selectedPreviewBeatmapLevel;
#elif BEAT_SABER_1_29_0
            view.selectedBeatmapLevel;
#else
            view.beatmapLevel;
#endif
        internal static NativeSongCollection Collection(CollectionCell cell) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            cell._annotatedBeatmapLevelCollection;
#else
            cell._beatmapLevelPack;
#endif
        internal static IDisposable Subscribe(LevelFilteringNavigationController view, Action<NativeSongPack> callback) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
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
