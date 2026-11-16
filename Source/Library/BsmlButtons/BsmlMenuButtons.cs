#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.MenuButtons;
#else
using BeatSaberMarkupLanguage.MenuButtons;
#endif

namespace Legato.Presentation {
    internal static class BsmlMenuButtons {
        internal static void Register(MenuButton button) => MenuButtons.Instance.RegisterButton(button);
        internal static void Unregister(MenuButton button) => MenuButtons.Instance.UnregisterButton(button);
    }
}
