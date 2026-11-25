#if BEAT_SABER_1_29_0
global using NativeVisualSettings = global::MainSettingsModelSO;
#elif LEGATO_MAIN_SETTINGS_HANDLER
global using NativeVisualSettings = global::BeatSaber.GameSettings.GraphicSettingsHandler;
#else
global using NativeVisualSettings = global::SettingsManager;
#endif

namespace Legato.Rendering {
    internal static class ScreenDisplacementSettings {
        internal static bool IsEnabled(NativeVisualSettings settings) =>
#if BEAT_SABER_1_29_0
            settings.screenDisplacementEffectsEnabled.value;
#elif LEGATO_MAIN_SETTINGS_HANDLER
            settings.TryGetCurrentPerformancePreset(out var preset) && preset.screenDisplacementEffects;
#else
            settings.settings.quality.screenDisplacementEffects;
#endif
    }
}
