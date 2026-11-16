using UnityEngine;
using System.Linq;
using Zenject;

namespace Legato.Settings {
    internal static class MutableGameSettings {
#if BEAT_SABER_1_29_0
        private static MainSettingsModelSO _settings;
#else
        private static SettingsManager _settings = SettingsManager.CreateUninitialized();
        private static SettingsApplicatorSO _applicator;
#endif
        internal static void Initialize(DiContainer container) {
#if BEAT_SABER_1_29_0
            _settings = container.TryResolve<MainSettingsModelSO>() ?? UnityEngine.Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().FirstOrDefault();
#else
            _settings = container.TryResolve<SettingsManager>();
            _applicator = container.TryResolve<SettingsApplicatorSO>();
#endif
        }
        internal static float Volume =>
#if BEAT_SABER_1_29_0
            _settings == null ? 1f : _settings.volume.value;
#else
            _settings.settings.audio.volume;
#endif
        internal static Vector3 RoomCenter {
            get =>
#if BEAT_SABER_1_29_0
                _settings == null ? Vector3.zero : _settings.roomCenter.value;
#else
                _settings.settings.room.center;
#endif
            set {
#if BEAT_SABER_1_29_0
                if (_settings != null) _settings.roomCenter.value = value;
#else
                _settings.settings.room.center = value;
                _applicator?.NotifyRoomTransformOffsetWasUpdated();
#endif
            }
        }
        internal static float RoomRotation {
            get =>
#if BEAT_SABER_1_29_0
                _settings == null ? 0f : _settings.roomRotation.value;
#else
                _settings.settings.room.rotation;
#endif
            set {
#if BEAT_SABER_1_29_0
                if (_settings != null) _settings.roomRotation.value = value;
#else
                _settings.settings.room.rotation = value;
                _applicator?.NotifyRoomTransformOffsetWasUpdated();
#endif
            }
        }
        internal static Vector3 ControllerPosition {
            get =>
#if BEAT_SABER_1_29_0
                _settings == null ? Vector3.zero : _settings.controllerPosition.value;
#else
                _settings.settings.controller.position;
#endif
            set {
#if BEAT_SABER_1_29_0
                if (_settings != null) _settings.controllerPosition.value = value;
#else
                _settings.settings.controller.position = value;
#endif
            }
        }
        internal static Vector3 ControllerRotation {
            get =>
#if BEAT_SABER_1_29_0
                _settings == null ? Vector3.zero : _settings.controllerRotation.value;
#else
                _settings.settings.controller.rotation;
#endif
            set {
#if BEAT_SABER_1_29_0
                if (_settings != null) _settings.controllerRotation.value = value;
#else
                _settings.settings.controller.rotation = value;
#endif
            }
        }
    }
}
