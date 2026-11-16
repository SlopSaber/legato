#if BEAT_SABER_1_29_0
using System;
using IPA.Utilities;
namespace SongCore.Utilities
{
    internal static class Accessors
    {
        internal static readonly FieldAccessor<SaberManager, SaberManager.InitData>.Accessor SaberManagerInitDataAccessor = FieldAccessor<SaberManager, SaberManager.InitData>.GetAccessor(nameof(SaberManager._initData));
        public static readonly FieldAccessor<LevelCollectionTableView, Action<LevelCollectionTableView, BeatmapLevel>>.Accessor TableViewDidSelectLevelEventAccessor =
            FieldAccessor<LevelCollectionTableView, Action<LevelCollectionTableView, BeatmapLevel>>.GetAccessor(nameof(LevelCollectionTableView.didSelectLevelEvent));

        public static readonly FieldAccessor<LevelCollectionViewController, Action<LevelCollectionViewController, BeatmapLevel>>.Accessor ViewControllerDidSelectLevelEventAccessor =
            FieldAccessor<LevelCollectionViewController, Action<LevelCollectionViewController, BeatmapLevel>>.GetAccessor(nameof(LevelCollectionViewController.didSelectLevelEvent));

        public static readonly FieldAccessor<StandardLevelDetailViewController, Action<StandardLevelDetailViewController, StandardLevelDetailViewController.ContentType>>.Accessor DidChangeContentEventAccessor =
            FieldAccessor<StandardLevelDetailViewController, Action<StandardLevelDetailViewController, StandardLevelDetailViewController.ContentType>>.GetAccessor(nameof(StandardLevelDetailViewController.didChangeContentEvent));
    }
}
#endif
