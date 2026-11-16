using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;

namespace Legato.XR {
    internal static class ControllerOffsetPatch {
        private static Func<bool> _useNative;
        internal static void Apply(Harmony harmony, Func<bool> useNative) {
            _useNative = useNative;
            var target = AccessTools.Method(typeof(VRController), "TryGetControllerOffset",
                new[] { typeof(IVRPlatformHelper), typeof(VRControllerTransformOffset), typeof(XRNode), typeof(Pose).MakeByRefType() });
            var prefix = AccessTools.Method(typeof(ControllerOffsetPatch), nameof(Prefix));
            if (target == null) {
                target = AccessTools.Method(typeof(VRController), "TryGetControllerOffset",
                    new[] { typeof(IVRPlatformHelper), typeof(VRControllerTransformOffset), typeof(XRNode).MakeByRefType(), typeof(Pose).MakeByRefType() });
                prefix = AccessTools.Method(typeof(ControllerOffsetPatch), nameof(PrefixByReference));
            }
            if (target != null) harmony.Patch(target, new HarmonyMethod(prefix));
        }
        private static bool Prefix(out Pose poseOffset) { poseOffset = Pose.identity; return _useNative(); }
        private static bool PrefixByReference(out Pose poseOffset) { poseOffset = Pose.identity; return _useNative(); }
    }
}
