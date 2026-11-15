#nullable enable

using UnityEngine;
#if !BEAT_SABER_1_29_0
using UnityEngine.SpatialTracking;
#endif

namespace Legato.XR {
    internal sealed class CameraPoseTracking {
#if !BEAT_SABER_1_29_0
        private readonly TrackedPoseDriver _driver;
#endif
        internal CameraPoseTracking(Camera camera) {
#if !BEAT_SABER_1_29_0
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
#if !BEAT_SABER_1_29_0
            if (_driver != null && !_driver.enabled) _driver.enabled = true;
#endif
        }
    }
}
