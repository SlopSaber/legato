#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable
using System.Collections.Generic;
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.MenuButtons;
#else
using BeatSaberMarkupLanguage.MenuButtons;
#endif

namespace Legato.Presentation {
    internal static class BsmlMenuButtons {
        private static readonly Dictionary<MenuButton, MenuButtons> Owners = new();

        internal static void Register(MenuButton button) {
            var owner = MenuButtons.Instance;
            owner.RegisterButton(button);
            Owners[button] = owner;
        }

        internal static void Unregister(MenuButton button) {
            if (!Owners.TryGetValue(button, out var owner)) return;
            owner.UnregisterButton(button);
            Owners.Remove(button);
        }
    }
}
