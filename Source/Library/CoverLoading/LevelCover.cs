using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps {
    internal static class LevelCover {
#if LEGATO_EARLY_STANDARD_MAPS
#nullable enable
        internal static Task<Sprite?> Load(IStandardLevel level) => Task.FromResult<Sprite?>(level.coverImage);
#elif LEGATO_BEFORE_MULTIPLAYER
#nullable enable
        internal static async Task<Sprite?> Load(BeatmapLevel level) {
            Texture2D texture = await level.GetCoverImageTexture2DAsync(CancellationToken.None).ConfigureAwait(true);
            return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
#else
        internal static Task<Sprite> Load(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level.GetCoverImageAsync(CancellationToken.None);
#elif LEGATO_CANCELLABLE_COVER_LOADING
            level.previewMediaData.GetCoverSpriteAsync(CancellationToken.None);
#else
            level.previewMediaData.GetCoverSpriteAsync();
#endif
#endif
    }
}
