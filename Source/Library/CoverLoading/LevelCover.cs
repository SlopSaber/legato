using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps {
    internal static class LevelCover {
        internal static Task<Sprite> Load(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level.GetCoverImageAsync(CancellationToken.None);
#else
            level.previewMediaData.GetCoverSpriteAsync();
#endif
    }
}
