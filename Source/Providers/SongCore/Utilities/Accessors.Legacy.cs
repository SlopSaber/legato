#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System;
using IPA.Utilities;
namespace SongCore.Utilities
{
    internal static class Accessors
    {
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static readonly FieldAccessor<SaberManager, SaberManager.InitData>.Accessor SaberManagerInitDataAccessor = FieldAccessor<SaberManager, SaberManager.InitData>.GetAccessor(nameof(SaberManager._initData));
#endif
        public static readonly FieldAccessor<LevelCollectionTableView, Action<LevelCollectionTableView, BeatmapLevel>>.Accessor TableViewDidSelectLevelEventAccessor =
            FieldAccessor<LevelCollectionTableView, Action<LevelCollectionTableView, BeatmapLevel>>.GetAccessor(nameof(LevelCollectionTableView.didSelectLevelEvent));

        public static readonly FieldAccessor<LevelCollectionViewController, Action<LevelCollectionViewController, BeatmapLevel>>.Accessor ViewControllerDidSelectLevelEventAccessor =
            FieldAccessor<LevelCollectionViewController, Action<LevelCollectionViewController, BeatmapLevel>>.GetAccessor(nameof(LevelCollectionViewController.didSelectLevelEvent));

        public static readonly FieldAccessor<StandardLevelDetailViewController, Action<StandardLevelDetailViewController, StandardLevelDetailViewController.ContentType>>.Accessor DidChangeContentEventAccessor =
#if LEGATO_BEFORE_MULTIPLAYER
            FieldAccessor<StandardLevelDetailViewController, Action<StandardLevelDetailViewController, StandardLevelDetailViewController.ContentType>>.GetAccessor(nameof(StandardLevelDetailViewController.didPresentContentEvent));
#else
            FieldAccessor<StandardLevelDetailViewController, Action<StandardLevelDetailViewController, StandardLevelDetailViewController.ContentType>>.GetAccessor(nameof(StandardLevelDetailViewController.didChangeContentEvent));
#endif
    }
}
#endif
