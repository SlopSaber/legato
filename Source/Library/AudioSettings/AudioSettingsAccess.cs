#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
global using NativeAudioSettings = global::MainSettingsModelSO;
#elif LEGATO_MAIN_SETTINGS_HANDLER
global using NativeAudioSettings = global::BeatSaber.GameSettings.MainSettingsHandler;
#else
global using NativeAudioSettings = global::SettingsManager;
#endif
namespace Legato.Audio {
    internal static class AudioSettingsAccess {
        internal static float Volume(NativeAudioSettings settings) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            settings.volume.value;
#elif LEGATO_MAIN_SETTINGS_HANDLER
            settings.instance.audioSettings.volume;
#else
            settings.settings.audio.volume;
#endif
#if LEGATO_MAIN_SETTINGS_HANDLER
        internal static float Latency(NativeAudioSettings settings, global::BeatSaber.GameSettings.GraphicSettingsHandler graphics) =>
            settings.instance.audioSettings.GetAudioLatency(graphics);
#else
        internal static float Latency(NativeAudioSettings settings) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            settings.audioLatency.value;
#else
            settings.settings.audio.latency;
#endif
#endif
    }
}
