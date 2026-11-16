using UnityEngine;

namespace Legato.Models;

internal static class ModelTextures
{
    internal static void Resize(Texture2D texture, int width, int height)
    {
#if BEAT_SABER_1_29_0
        texture.Resize(width, height);
#else
        texture.Reinitialize(width, height);
#endif
    }
}
