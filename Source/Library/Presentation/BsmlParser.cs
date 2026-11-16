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
    internal static class BsmlParser {
        internal static BSMLParser Instance {
            get {
#if !LEGATO_BSML_CURRENT && (BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1)
                return BSMLParser.instance;
#else
                return BSMLParser.Instance;
#endif
            }
        }
    }
}
