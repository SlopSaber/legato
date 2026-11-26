using TMPro;
#if !LEGATO_EARLY_VR_UI
using UnityEngine.TextCore.LowLevel;
#endif

namespace Legato.Unity
{
    /// <summary>
    /// Adapts font rendering and wrapping to the installed TextMeshPro version.
    /// </summary>
    internal static class TextCompatibility
    {
#if !LEGATO_EARLY_VR_UI
        /// <summary>
        /// Gets the render mode used when importing color glyphs.
        /// </summary>
        internal static GlyphRenderMode ColorGlyphMode =>
#if LEGATO_LEGACY_TEXTMESH_PRO
            GlyphRenderMode.RASTER;
#else
            GlyphRenderMode.COLOR;
#endif
#endif

        /// <summary>
        /// Enables or disables word wrapping on a text component.
        /// </summary>
        /// <param name="text">The text component.</param>
        /// <param name="enabled">Whether to wrap text.</param>
        internal static void SetWrapping(TMP_Text text, bool enabled)
        {
#if LEGATO_LEGACY_TEXTMESH_PRO || LEGATO_EARLY_VR_UI
            text.enableWordWrapping = enabled;
#else
            text.textWrappingMode = enabled ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
#endif
        }
    }
}
