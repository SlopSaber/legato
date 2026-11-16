using UnityEngine;

namespace Legato.Settings {
    internal static class NativeGameSettings {
        internal static Vector3 RoomCenter(MainSystemInit init) =>
#if BEAT_SABER_1_29_0
            init._mainSettingsModel.roomCenter.value;
#else
            init._settingsManager.settings.room.center;
#endif
        internal static float RoomRotation(MainSystemInit init) =>
#if BEAT_SABER_1_29_0
            init._mainSettingsModel.roomRotation.value;
#else
            init._settingsManager.settings.room.rotation;
#endif
        internal static bool MainEffectsDisabled(MainSystemInit init) =>
#if BEAT_SABER_1_29_0
            init._mainEffectGraphicsSettingsPresets.presets[init._mainSettingsModel.mainEffectGraphicsSettings.value].mainEffect == null;
#else
            init._settingsManager.settings.quality.mainEffect == BeatSaber.Settings.QualitySettings.MainEffectOption.Off;
#endif
    }
}
