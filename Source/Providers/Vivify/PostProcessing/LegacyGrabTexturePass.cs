#if BEAT_SABER_1453
namespace Vivify.PostProcessing;

internal sealed class LegacyGrabTexturePass : Legato.Rendering.LegacyGrabTexturePass
{
    internal LegacyGrabTexturePass()
        : base("Vivify legacy grab texture")
    {
    }
}
#endif
