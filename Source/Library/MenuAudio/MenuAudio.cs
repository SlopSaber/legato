#nullable enable

using IPA.Utilities;
using UnityEngine;

namespace Legato.Audio {
    internal static class MenuAudio {
        internal static AudioClip DefaultClip(SongPreviewPlayer player) => player.GetField<AudioClip, SongPreviewPlayer>("_defaultAudioClip");

#if LEGATO_BEFORE_MULTIPLAYER
        internal static AudioSource? ActivePreviewSource(SongPreviewPlayer player) {
            var sources = player.GetField<AudioSource[], SongPreviewPlayer>("_audioSources");
            int channel = player.GetField<int, SongPreviewPlayer>("_activeChannel");
            return sources != null && (uint)channel < (uint)sources.Length ? sources[channel] : null;
        }

        internal static ColorScheme? PreviewColorScheme(PlayerData player, ColorManager? colors) {
            var settings = player.colorSchemesSettings;
            if (settings.overrideDefaultColors) return settings.GetSelectedColorScheme();
            return colors == null ? null : colors.GetField<ColorSchemeSO, ColorManager>("_defaultColorScheme")?.colorScheme;
        }
#endif

        internal static void ProcessResultDestination(ResultsViewController controller) {
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
            if (controller._menuDestinationRequest != null)
                controller.ProcessMenuDestinationRequest(controller._menuDestinationRequest);
#endif
        }
    }
}
