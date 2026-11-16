#if BEAT_SABER_1_29_0
global using NativeVisualSettings = global::MainSettingsModelSO;
#else
global using NativeVisualSettings = global::SettingsManager;
#endif

namespace Legato.Rendering {
    internal static class ScreenDisplacementSettings {
        internal static bool IsEnabled(NativeVisualSettings settings) =>
#if BEAT_SABER_1_29_0
            settings.screenDisplacementEffectsEnabled.value;
#else
            settings.settings.quality.screenDisplacementEffects;
#endif
    }
}
