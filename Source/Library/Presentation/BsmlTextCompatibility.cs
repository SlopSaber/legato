#nullable enable

using TMPro;
using BeatSaberMarkupLanguage;

namespace Legato.Presentation {
    internal static class BsmlTextCompatibility {
        internal static void AssignMainFont(TextMeshProUGUI text) {
            text.font = BeatSaberUI.MainTextFont;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            text.fontSharedMaterial = BeatSaberUI.MainUIFontMaterial;
#else
            text.fontSharedMaterial = text.font.material;
#endif
        }
    }
}
