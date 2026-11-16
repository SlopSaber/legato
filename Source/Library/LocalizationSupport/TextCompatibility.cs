using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace Legato.Unity
{
    internal static class TextCompatibility
    {
        internal static GlyphRenderMode ColorGlyphMode =>
#if BEAT_SABER_1_29_0
            GlyphRenderMode.RASTER;
#else
            GlyphRenderMode.COLOR;
#endif

        internal static void SetWrapping(TMP_Text text, bool enabled)
        {
#if BEAT_SABER_1_29_0
            text.enableWordWrapping = enabled;
#else
            text.textWrappingMode = enabled ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
#endif
        }
    }
}
