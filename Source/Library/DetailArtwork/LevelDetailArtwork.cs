using HMUI;
using IPA.Utilities;

namespace Legato.Presentation
{
    internal static class LevelDetailArtwork
    {
#if LEGATO_BEFORE_MULTIPLAYER
#nullable enable
        internal static System.Threading.Tasks.Task<UnityEngine.Sprite?> Cover(BeatmapLevel level) => Beatmaps.LevelCover.Load(level);
#nullable restore
#else
        internal static System.Threading.Tasks.Task<UnityEngine.Sprite> Cover(BeatmapLevel level) => Beatmaps.LevelCover.Load(level);
#endif

        internal static readonly FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.Accessor DetailView = FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.GetAccessor("_standardLevelDetailView");
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static readonly FieldAccessor<StandardLevelDetailView, LevelBar>.Accessor LevelBar = FieldAccessor<StandardLevelDetailView, LevelBar>.GetAccessor("_levelBar");
        internal static readonly FieldAccessor<LevelBar, ImageView>.Accessor Artwork = FieldAccessor<LevelBar, ImageView>.GetAccessor("_songArtworkImageView");
#endif
    }
}
