#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage;
#else
using BeatSaberMarkupLanguage;
#endif

namespace Legato.Presentation {
    internal static class BsmlUiAudio {
        internal static BasicUIAudioManager Manager => BeatSaberUI.BasicUIAudioManager;
    }
}
