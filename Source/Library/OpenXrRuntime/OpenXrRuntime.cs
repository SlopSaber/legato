#if LEGATO_EARLY_STANDARD_MAPS || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
#define LEGATO_LEGACY_XR_RUNTIME
#endif

using System.Collections.Generic;
using System.Reflection;
using UnityEngine.XR;
#if !LEGATO_LEGACY_XR_RUNTIME
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

namespace Legato.XR {
    internal static class OpenXrRuntime {
        internal static string SettingsName =>
#if LEGATO_LEGACY_XR_RUNTIME
            null;
#else
            OpenXRSettings.Instance == null ? null : OpenXRSettings.Instance.name;
#endif
        internal static System.Collections.IEnumerator RestartLoader() {
#if !LEGATO_LEGACY_XR_RUNTIME
            yield return null;
            var manager = UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager;
            if (manager.isInitializationComplete) {
                manager.DeinitializeLoader();
                yield return null;
            }
            yield return manager.InitializeLoader();
            manager.StartSubsystems();
#else
            yield break;
#endif
        }
        internal static string Name =>
#if LEGATO_LEGACY_XR_RUNTIME
            XRSettings.loadedDeviceName;
#else
            OpenXRRuntime.name;
#endif
        internal static string Version =>
#if LEGATO_LEGACY_XR_RUNTIME
            string.Empty;
#else
            OpenXRRuntime.version;
#endif
        internal static bool EyeGazeEnabled =>
#if LEGATO_LEGACY_XR_RUNTIME
            false;
#else
            OpenXRRuntime.IsExtensionEnabled(EyeGazeInteraction.extensionString);
#endif
        internal static void EnableEyeGaze() {
#if !LEGATO_LEGACY_XR_RUNTIME
            var feature = OpenXRSettings.Instance?.GetFeature<EyeGazeInteraction>();
            if (feature != null) feature.enabled = true;
#endif
        }
        internal static IEnumerable<MethodBase> LoaderInitializationMethods {
            get {
#if !LEGATO_LEGACY_XR_RUNTIME
                yield return typeof(OpenXRLoaderBase).GetMethod(nameof(OpenXRLoaderBase.Initialize));
#else
                yield break;
#endif
            }
        }
    }
}
