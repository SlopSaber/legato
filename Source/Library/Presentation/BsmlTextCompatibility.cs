#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

using TMPro;
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage;
#else
using BeatSaberMarkupLanguage;
#endif

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
