#nullable enable
using BeatSaberMarkupLanguage.MenuButtons;

namespace Legato.Presentation {
    internal static class BsmlMenuButtons {
        internal static void Register(MenuButton button) => MenuButtons.Instance.RegisterButton(button);
        internal static void Unregister(MenuButton button) => MenuButtons.Instance.UnregisterButton(button);
    }
}
