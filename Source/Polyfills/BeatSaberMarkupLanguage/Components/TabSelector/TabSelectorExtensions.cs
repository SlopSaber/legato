#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.Components;
#else
using BeatSaberMarkupLanguage.Components;
#endif
using HMUI;

namespace Legato {
    internal static class TabSelectorExtensions {
        internal static TextSegmentedControl GetTextSegmentedControl(this TabSelector tabSelector) {
#if !LEGATO_BSML_CURRENT && (BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1)
            return tabSelector.textSegmentedControl;
#else
            return tabSelector.TextSegmentedControl;
#endif
        }
    }
}
