#if LEGATO_BEFORE_MULTIPLAYER
global using IVRPlatformHelper = global::VRPlatformHelper;
#endif
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
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private sealed class RotationState { public RotationState() { } internal Quaternion value = Quaternion.identity; }
        private static readonly ConditionalWeakTable<VRController, RotationState> Rotations = new();
#else
        private static readonly FieldAccessor<VRController, Quaternion>.Accessor Rotation = FieldAccessor<VRController, Quaternion>.GetAccessor("_lastTrackedRotation");
#endif
        internal static int NodeIndex(VRController controller) =>
#if LEGATO_CONTROLLER_NODE_IDX
            controller.nodeIdx;
#else
            controller.nodeIndex;
#endif
        internal static bool CanConvertSettings(IVRPlatformHelper platform) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            platform != null;
#else
            platform is UnityXRHelper;
#endif
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private sealed class OffsetState {
            public OffsetState() { }
            internal bool leftReady, rightReady;
            internal Pose left, right;
        }
        private static ConditionalWeakTable<IVRPlatformHelper, OffsetState> Offsets = new();
#endif
        internal static Pose BuiltInOffset(IVRPlatformHelper platform, XRNode node) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var cached = Offsets.GetOrCreateValue(platform);
            bool left = node == XRNode.LeftHand;
            if (left ? cached.leftReady : cached.rightReady) return left ? cached.left : cached.right;
            var probe = new GameObject("Controller offset probe") { hideFlags = HideFlags.HideAndDontSave };
            Pose pose;
            try {
#if LEGATO_BEFORE_MULTIPLAYER
                platform.AdjustPlatformSpecificControllerTransform(node, probe.transform, Vector3.zero, Vector3.zero);
#else
                platform.AdjustControllerTransform(node, probe.transform, Vector3.zero, Vector3.zero);
#endif
                pose = new Pose(probe.transform.localPosition, probe.transform.localRotation);
            } finally { UnityEngine.Object.Destroy(probe); }
            if (left) { cached.left = pose; cached.leftReady = true; }
            else { cached.right = pose; cached.rightReady = true; }
            return pose;
#else
            platform.TryGetPoseOffsetForNode(node, out var pose);
            return pose;
#endif
        }
        internal static void HideHint(HMUI.HoverHintController controller, HMUI.HoverHint hint) {
#if LEGATO_PARAMETERLESS_HIDE_HINT
            controller.HideHintInstant();
#else
            controller.HideHintInstant(hint);
#endif
        }
        internal static bool ReadPose(VRController controller, out Vector3 position, out Quaternion rotation) {
            var platform = Platform(ref controller);
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (platform != null && platform.GetNodePose(controller.node, NodeIndex(controller), out position, out rotation)) {
                Position(ref controller) = position;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                Rotations.GetOrCreateValue(controller).value = rotation;
#else
                Rotation(ref controller) = rotation;
#endif
                return true;
            }
            position = Position(ref controller);
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            rotation = Rotations.GetOrCreateValue(controller).value;
#else
            rotation = Rotation(ref controller);
#endif
            return false;
        }
        internal static void SetLocalPose(Transform transform, Vector3 position, Quaternion rotation) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            transform.localPosition = position;
            transform.localRotation = rotation;
#else
            transform.SetLocalPositionAndRotation(position, rotation);
#endif
        }
        internal static Pose LegacyModelOffset(IVRPlatformHelper platform, XRNode node) =>
#if LEGATO_LEGACY_CONTROLLER_MODEL_OFFSET
            platform.GetRootPositionOffsetForLegacyNodePose(node);
#else
            Pose.identity;
#endif
        internal static IDisposable SubscribeChanges(DiContainer container, Action callback) => new Subscription(container, callback);
        private sealed class Subscription : IDisposable {
            private readonly Action _callback;
            private bool _disposed;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            private readonly IXRSystemState _state;
#endif
            internal Subscription(DiContainer container, Action callback) {
                _callback = callback;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
                _state = container.Resolve<IXRSystemState>();
                _state.AddListener(StateChanged);
#else
                InputDevices.deviceConnected += DeviceChanged;
                InputDevices.deviceDisconnected += DeviceChanged;
                InputDevices.deviceConfigChanged += DeviceChanged;
#endif
            }
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            private void StateChanged(XRSystemEventType type) {
                if (type == XRSystemEventType.ControllersDidChangeReference || type == XRSystemEventType.ControllersDidDisconnect) _callback();
            }
#else
            private void DeviceChanged(InputDevice _) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                Offsets = new();
#endif
                _callback();
            }
#endif
            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
                _state.RemoveListener(StateChanged);
#else
                InputDevices.deviceConnected -= DeviceChanged;
                InputDevices.deviceDisconnected -= DeviceChanged;
                InputDevices.deviceConfigChanged -= DeviceChanged;
#endif
            }
        }
    }
}
