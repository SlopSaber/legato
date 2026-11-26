#nullable enable

using UnityEngine;
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
using UnityEngine.SpatialTracking;
#endif

namespace Legato.XR {
    internal sealed class CameraPoseTracking {
#if LEGATO_BEFORE_MULTIPLAYER
        private readonly Camera _camera;
        private bool _tracking;
#elif !BEAT_SABER_1_29_0
        private readonly TrackedPoseDriver _driver;
#endif
        internal CameraPoseTracking(Camera camera) {
#if LEGATO_BEFORE_MULTIPLAYER
            _camera = camera;
            EnsureEnabled();
#elif !BEAT_SABER_1_29_0
            _driver = camera.GetComponent<TrackedPoseDriver>();
            if (_driver == null) _driver = camera.gameObject.AddComponent<TrackedPoseDriver>();
            _driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            _driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            _driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            _driver.UseRelativeTransform = false;
            _driver.enabled = true;
#endif
        }

        internal void EnsureEnabled() {
#if LEGATO_BEFORE_MULTIPLAYER
            if (!UnityEngine.XR.XRSettings.enabled) {
                _tracking = false;
            } else if (!_tracking && _camera != null && _camera.isActiveAndEnabled) {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(_camera, false);
                _tracking = true;
            }
#elif !BEAT_SABER_1_29_0
            if (_driver != null && !_driver.enabled) _driver.enabled = true;
#endif
        }
    }
}
