using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace Legato.Unity
{
    /// <summary>
    /// Adapts font rendering and wrapping to the installed TextMeshPro version.
    /// </summary>
    internal static class TextCompatibility
    {
        /// <summary>
        /// Gets the render mode used when importing color glyphs.
        /// </summary>
        internal static GlyphRenderMode ColorGlyphMode =>
#if BEAT_SABER_1_29_0
            GlyphRenderMode.RASTER;
#else
            GlyphRenderMode.COLOR;
#endif

        /// <summary>
        /// Enables or disables word wrapping on a text component.
        /// </summary>
        /// <param name="text">The text component.</param>
        /// <param name="enabled">Whether to wrap text.</param>
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
