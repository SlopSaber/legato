#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable
using System.Threading.Tasks;
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.Settings;
#else
using BeatSaberMarkupLanguage.Settings;
#endif
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.Util;
#else
using BeatSaberMarkupLanguage.Util;
#endif

namespace Legato.Presentation {
    internal static class BsmlSettingsMenu {
        internal static Task WaitForReady() => MainMenuAwaiter.WaitForMainMenuAsync();

        internal static void Add(string title, string resource, object host) =>
            BSMLSettings.Instance.AddSettingsMenu(title, resource, host);

        internal static void Remove(object host) => BSMLSettings.Instance.RemoveSettingsMenu(host);

        internal static void Add(BSMLSettings settings, string title, string resource, object host) =>
            settings.AddSettingsMenu(title, resource, host);

        internal static void Remove(BSMLSettings settings, object host) => settings.RemoveSettingsMenu(host);
    }
}
