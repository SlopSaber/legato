#if BEAT_SABER_1453
using System.Collections.Generic;

namespace Vivify.PostProcessing;

internal sealed class LegacyGrabTexturePass : Legato.Rendering.LegacyGrabTexturePass
{
    internal LegacyGrabTexturePass(IReadOnlyCollection<string> namedTextures)
        : base("Vivify legacy grab texture", namedTextures)
    {
    }
}
#endif
