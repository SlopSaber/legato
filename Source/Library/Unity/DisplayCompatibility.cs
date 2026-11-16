#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Legato.Unity {
    internal static class DisplayCompatibility {
        internal static void GetDisplays(List<XRDisplaySubsystem> displays) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            SubsystemManager.GetSubsystems(displays);
#else
            SubsystemManager.GetInstances(displays);
#endif
        }

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
