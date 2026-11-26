using UnityEngine;

namespace Legato.Settings {
    internal static class NativeGameSettings {
        internal static Vector3 RoomCenter(MainSystemInit init) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            init._mainSettingsModel.roomCenter.value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
            init._mainSettingsHandler.instance.roomCenter;
#else
            init._settingsManager.settings.room.center;
#endif
        internal static float RoomRotation(MainSystemInit init) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            init._mainSettingsModel.roomRotation.value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
            init._mainSettingsHandler.instance.roomRotation;
#else
            init._settingsManager.settings.room.rotation;
#endif
        internal static bool MainEffectsDisabled(MainSystemInit init) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            init._mainEffectGraphicsSettingsPresets.presets[init._mainSettingsModel.mainEffectGraphicsSettings.value].mainEffect == null;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
            init._graphicSettingsHandler.TryGetCurrentPerformancePreset(out var preset) &&
                preset.mainEffectGraphics == BeatSaber.PerformancePresets.MainEffectPreset.Off;
#else
            init._settingsManager.settings.quality.mainEffect == BeatSaber.Settings.QualitySettings.MainEffectOption.Off;
#endif
    }
}
