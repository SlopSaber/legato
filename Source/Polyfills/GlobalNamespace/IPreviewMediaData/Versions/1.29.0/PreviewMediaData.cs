#nullable enable

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato {
    internal class PreviewMediaData {
        private readonly IPreviewBeatmapLevel _level;

        internal PreviewMediaData(IPreviewBeatmapLevel level) {
            _level = level;
        }

#if LEGATO_COVER_TEXTURE
        public Task<Texture2D> GetCoverTextureAsync() => _level.GetCoverImageTexture2DAsync(CancellationToken.None);
#else
        public Task<Sprite> GetCoverSpriteAsync() => _level.GetCoverImageAsync(CancellationToken.None);
#endif
    }
}
