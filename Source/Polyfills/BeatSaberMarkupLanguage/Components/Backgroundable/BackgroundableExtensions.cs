#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.Components;
#else
using BeatSaberMarkupLanguage.Components;
#endif
using UnityEngine.UI;

namespace Legato {
    internal static class BackgroundableExtensions {
        internal static Image GetBackground(this Backgroundable backgroundable) {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            return backgroundable.background;
#else
            return backgroundable.Background;
#endif
        }
    }
}
