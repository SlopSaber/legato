#if BEAT_SABER_1453
namespace Vivify.PostProcessing;

internal sealed class LegacyLightingPass : Legato.Rendering.LegacyLightingPass
{
    internal LegacyLightingPass()
        : base("Vivify legacy lighting", true)
    {
    }
}
#endif
