using TMPro;
using UnityEngine.TextCore;

namespace Legato.Text {
    internal static class FontGlyphs {
        internal static TMP_Character CreateCharacter(uint codepoint, TMP_FontAsset font, Glyph glyph) =>
#if BEAT_SABER_1_29_0
            new TMP_Character(codepoint, glyph);
#else
            new TMP_Character(codepoint, font, glyph);
#endif
    }
}
