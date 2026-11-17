using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;
using System.Reflection;

namespace Legato.XR {
    internal static class ControllerOffsetPatch {
        private static Func<bool> _useNative = static () => true;
        internal static void Apply(Harmony harmony, Func<bool> useNative) {
            _useNative = useNative;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var target = typeof(VRController).GetMethod("TryGetControllerOffset", flags, null,
                new[] { typeof(IVRPlatformHelper), typeof(VRControllerTransformOffset), typeof(XRNode), typeof(Pose).MakeByRefType() }, null);
            var prefix = AccessTools.Method(typeof(ControllerOffsetPatch), nameof(Prefix));
            if (target == null) {
                target = typeof(VRController).GetMethod("TryGetControllerOffset", flags, null,
                    new[] { typeof(IVRPlatformHelper), typeof(VRControllerTransformOffset), typeof(XRNode).MakeByRefType(), typeof(Pose).MakeByRefType() }, null);
                prefix = AccessTools.Method(typeof(ControllerOffsetPatch), nameof(PrefixByReference));
            }
            if (target != null) harmony.Patch(target, new HarmonyMethod(prefix));
        }
        private static bool Prefix(out Pose poseOffset) { poseOffset = Pose.identity; return _useNative(); }
        private static bool PrefixByReference(out Pose poseOffset) { poseOffset = Pose.identity; return _useNative(); }
    }
}
