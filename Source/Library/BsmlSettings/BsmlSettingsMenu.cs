#nullable enable
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;

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
