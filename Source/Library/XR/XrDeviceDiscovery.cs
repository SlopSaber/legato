#nullable enable

using System.Collections.Generic;
using UnityEngine.XR;

namespace Legato.XR {
    public static class XrDeviceDiscovery {
        public static string RuntimeName {
            get {
#if BEAT_SABER_1_29_0
                return string.Empty;
#else
                return UnityEngine.XR.OpenXR.OpenXRRuntime.name ?? string.Empty;
#endif
            }
        }

        public static string LegacyHeadsetModel {
            get {
#if BEAT_SABER_1_29_0
#pragma warning disable CS0618
                return XRDevice.model;
#pragma warning restore CS0618
#else
                return string.Empty;
#endif
            }
        }

        public static string DescribeHeadset(string? steamHeadsetName) {
#if BEAT_SABER_1_29_0
            string device = "(xrdevice):" + LegacyHeadsetModel;
            if (steamHeadsetName != null) device = $"{device}:(steamcfg):{steamHeadsetName}";
            return "legacy:" + device;
#else
            string runtime = RuntimeName;
            string? headset = DescribeNode(XRNode.Head);
            if (runtime.IndexOf("steam", System.StringComparison.OrdinalIgnoreCase) >= 0 && steamHeadsetName != null)
                headset = $"{headset}:(steamcfg):{steamHeadsetName}";
            return $"{runtime}:{headset}";
#endif
        }

        public static string DescribeController(XRNode node, InputDeviceCharacteristics hand) {
#if BEAT_SABER_1_29_0
            string? name = GetControllerDeviceName(hand);
            string device = name == null ? string.Empty : "(inputdevice):" + name;
            string? nodeName = DescribeNode(node);
            if (nodeName != null) device = string.IsNullOrEmpty(device) ? nodeName : $"{device}:{nodeName}";
            return !string.IsNullOrEmpty(device) ? "legacy:" + device : "legacy:unknown";
#else
            return $"{RuntimeName}:{DescribeNode(node)}";
#endif
        }

        private static string? DescribeNode(XRNode node) {
            string? name = GetNodeDeviceName(node);
            return name == null ? null : "(xrnode):" + name;
        }

        public static string? GetNodeDeviceName(XRNode node) {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, devices);
            return devices.Count == 0 ? null : devices[0].name;
        }

        public static string? GetControllerDeviceName(InputDeviceCharacteristics hand) {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | hand, devices);
            return devices.Count == 0 ? null : devices[0].name;
        }
    }
}
