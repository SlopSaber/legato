using UnityEngine;
using System.Linq;
using Zenject;
#if LEGATO_APPLY_PERFORMANCE_PRESET
using BeatSaber.GameSettings;
#endif

namespace Legato.Settings {
    internal static class MutableGameSettings {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private static MainSettingsModelSO _settings;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
        private static MainSettingsHandler _settings;
        private static SettingsApplicatorSO _applicator;
#else
        private static SettingsManager _settings = SettingsManager.CreateUninitialized();
        private static SettingsApplicatorSO _applicator;
#endif
        internal static void Initialize(DiContainer container) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            _settings = container.TryResolve<MainSettingsModelSO>() ?? UnityEngine.Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().FirstOrDefault();
#elif LEGATO_APPLY_PERFORMANCE_PRESET
            _settings = container.TryResolve<MainSettingsHandler>();
            _applicator = container.TryResolve<SettingsApplicatorSO>();
#else
            _settings = container.TryResolve<SettingsManager>();
            _applicator = container.TryResolve<SettingsApplicatorSO>();
#endif
        }
        internal static float Volume =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            _settings == null ? 1f : _settings.volume.value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
            _settings?.instance?.audioSettings.volume ?? 1f;
#else
            _settings.settings.audio.volume;
#endif
        internal static Vector3 RoomCenter {
            get =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                _settings == null ? Vector3.zero : _settings.roomCenter.value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                _settings?.instance?.roomCenter ?? Vector3.zero;
#else
                _settings.settings.room.center;
#endif
            set {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                if (_settings != null) _settings.roomCenter.value = value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                if (_settings?.instance is not { } settings) return;
                settings.roomCenter = value;
                _applicator?.NotifyRoomTransformOffsetWasUpdated();
#else
                _settings.settings.room.center = value;
                _applicator?.NotifyRoomTransformOffsetWasUpdated();
#endif
            }
        }
        internal static float RoomRotation {
            get =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                _settings == null ? 0f : _settings.roomRotation.value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                _settings?.instance?.roomRotation ?? 0f;
#else
                _settings.settings.room.rotation;
#endif
            set {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                if (_settings != null) _settings.roomRotation.value = value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                if (_settings?.instance is not { } settings) return;
                settings.roomRotation = value;
                _applicator?.NotifyRoomTransformOffsetWasUpdated();
#else
                _settings.settings.room.rotation = value;
                _applicator?.NotifyRoomTransformOffsetWasUpdated();
#endif
            }
        }
        internal static Vector3 ControllerPosition {
            get =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                _settings == null ? Vector3.zero : _settings.controllerPosition.value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                _settings?.instance?.controllerSettings.positionOffset ?? Vector3.zero;
#else
                _settings.settings.controller.position;
#endif
            set {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                if (_settings != null) _settings.controllerPosition.value = value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                if (_settings?.instance is { } settings) settings.controllerSettings.positionOffset = value;
#else
                _settings.settings.controller.position = value;
#endif
            }
        }
        internal static Vector3 ControllerRotation {
            get =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                _settings == null ? Vector3.zero : _settings.controllerRotation.value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                _settings?.instance?.controllerSettings.rotationOffset ?? Vector3.zero;
#else
                _settings.settings.controller.rotation;
#endif
            set {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                if (_settings != null) _settings.controllerRotation.value = value;
#elif LEGATO_APPLY_PERFORMANCE_PRESET
                if (_settings?.instance is { } settings) settings.controllerSettings.rotationOffset = value;
#else
                _settings.settings.controller.rotation = value;
#endif
            }
        }
    }
}
