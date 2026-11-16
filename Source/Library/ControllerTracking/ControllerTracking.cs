using System;
using System.Runtime.CompilerServices;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace Legato.XR {
    internal static class ControllerTracking {
        private static readonly FieldAccessor<VRController, IVRPlatformHelper>.Accessor Platform = FieldAccessor<VRController, IVRPlatformHelper>.GetAccessor("_vrPlatformHelper");
        private static readonly FieldAccessor<VRController, Vector3>.Accessor Position = FieldAccessor<VRController, Vector3>.GetAccessor("_lastTrackedPosition");
#if BEAT_SABER_1_29_0
        private sealed class RotationState { internal Quaternion value = Quaternion.identity; }
        private static readonly ConditionalWeakTable<VRController, RotationState> Rotations = new();
#else
        private static readonly FieldAccessor<VRController, Quaternion>.Accessor Rotation = FieldAccessor<VRController, Quaternion>.GetAccessor("_lastTrackedRotation");
#endif
        internal static bool ReadPose(VRController controller, out Vector3 position, out Quaternion rotation) {
            var platform = Platform(ref controller);
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (platform != null && platform.GetNodePose(controller.node,
#if BEAT_SABER_1_29_0
                controller.nodeIdx,
#else
                controller.nodeIndex,
#endif
                out position, out rotation)) {
                Position(ref controller) = position;
#if BEAT_SABER_1_29_0
                Rotations.GetOrCreateValue(controller).value = rotation;
#else
                Rotation(ref controller) = rotation;
#endif
                return true;
            }
            position = Position(ref controller);
#if BEAT_SABER_1_29_0
            rotation = Rotations.GetOrCreateValue(controller).value;
#else
            rotation = Rotation(ref controller);
#endif
            return false;
        }
        internal static void SetLocalPose(Transform transform, Vector3 position, Quaternion rotation) {
#if BEAT_SABER_1_29_0
            transform.localPosition = position;
            transform.localRotation = rotation;
#else
            transform.SetLocalPositionAndRotation(position, rotation);
#endif
        }
        internal static Pose LegacyModelOffset(IVRPlatformHelper platform, XRNode node) =>
#if BEAT_SABER_1_29_0
            Pose.identity;
#else
            platform.GetRootPositionOffsetForLegacyNodePose(node);
#endif
        internal static IDisposable SubscribeChanges(DiContainer container, Action callback) => new Subscription(container, callback);
        private sealed class Subscription : IDisposable {
            private readonly Action _callback;
            private bool _disposed;
#if !BEAT_SABER_1_29_0
            private readonly IXRSystemState _state;
#endif
            internal Subscription(DiContainer container, Action callback) {
                _callback = callback;
#if BEAT_SABER_1_29_0
                InputDevices.deviceConnected += DeviceChanged;
                InputDevices.deviceDisconnected += DeviceChanged;
                InputDevices.deviceConfigChanged += DeviceChanged;
#else
                _state = container.Resolve<IXRSystemState>();
                _state.AddListener(StateChanged);
#endif
            }
#if BEAT_SABER_1_29_0
            private void DeviceChanged(InputDevice _) => _callback();
#else
            private void StateChanged(XRSystemEventType type) {
                if (type == XRSystemEventType.ControllersDidChangeReference || type == XRSystemEventType.ControllersDidDisconnect) _callback();
            }
#endif
            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
#if BEAT_SABER_1_29_0
                InputDevices.deviceConnected -= DeviceChanged;
                InputDevices.deviceDisconnected -= DeviceChanged;
                InputDevices.deviceConfigChanged -= DeviceChanged;
#else
                _state.RemoveListener(StateChanged);
#endif
            }
        }
    }
}
