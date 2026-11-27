#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Legato.Unity {
    internal static class DisplayCompatibility {
        internal static float HeadsetRefreshRate() {
#if LEGATO_EARLY_STANDARD_MAPS
            return XRSettings.isDeviceActive ? LegacyRefreshRate : 0f;
#else
            var displays = new List<XRDisplaySubsystem>();
            GetDisplays(displays);
            foreach (var display in displays)
                if (display.running && display.TryGetDisplayRefreshRate(out var rate) && rate > 0 && !float.IsInfinity(rate)) return rate;
            return HasRunningDisplay(displays) ? LegacyRefreshRate : 0f;
#endif
        }

        internal static double RefreshRate(Resolution resolution) =>
#if LEGATO_INTEGER_REFRESH_RATE || LEGATO_EARLY_STANDARD_MAPS
            resolution.refreshRate;
#else
            resolution.refreshRateRatio.value;
#endif

#if !LEGATO_EARLY_STANDARD_MAPS
        internal static void GetDisplays(List<XRDisplaySubsystem> displays) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3 || LEGATO_UNITY_6_OBJECT_QUERIES
            SubsystemManager.GetSubsystems(displays);
#else
            SubsystemManager.GetInstances(displays);
#endif
        }

        internal static bool HasRunningDisplay(List<XRDisplaySubsystem> displays) {
            foreach (XRDisplaySubsystem display in displays)
                if (display != null && display.running) return true;
#if LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
            return XRSettings.isDeviceActive;
#else
            return false;
#endif
        }
#endif

        internal static float LegacyRefreshRate {
            get {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
                return 0f;
#else
                return XRDevice.refreshRate;
#endif
            }
        }
    }
}
