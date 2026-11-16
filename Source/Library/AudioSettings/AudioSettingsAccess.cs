#if BEAT_SABER_1_29_0
global using NativeAudioSettings = global::MainSettingsModelSO;
#else
global using NativeAudioSettings = global::SettingsManager;
#endif
namespace Legato.Audio {
    internal static class AudioSettingsAccess {
        internal static float Latency(NativeAudioSettings settings) =>
#if BEAT_SABER_1_29_0
            settings.audioLatency.value;
#else
            settings.settings.audio.latency;
#endif
    }
}
