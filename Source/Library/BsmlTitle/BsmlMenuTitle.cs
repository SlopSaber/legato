#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage;
#else
using BeatSaberMarkupLanguage;
#endif
using HMUI;
using IPA.Utilities;

namespace Legato.Presentation {
    internal static class BsmlMenuTitle {
        internal static ViewController Get() => BeatSaberUI.DiContainer.Resolve<HierarchyManager>()
            .GetField<ScreenSystem, HierarchyManager>("_screenSystem").titleViewController;
    }
}
