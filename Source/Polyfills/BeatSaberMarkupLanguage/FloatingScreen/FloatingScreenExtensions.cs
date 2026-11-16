#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.FloatingScreen;
#else
using BeatSaberMarkupLanguage.FloatingScreen;
#endif
using UnityEngine;

namespace Legato {
    internal static class FloatingScreenExtensions {
        internal static GameObject GetHandle(this FloatingScreen screen) {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            return screen.handle;
#else
            return screen.Handle;
#endif
        }
    }
}
