using System.Collections.Generic;
using System.Reflection;
using UnityEngine.XR;
#if !BEAT_SABER_1_29_0
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

namespace Legato.XR {
    internal static class OpenXrRuntime {
        internal static string SettingsName =>
#if BEAT_SABER_1_29_0
            null;
#else
            OpenXRSettings.Instance == null ? null : OpenXRSettings.Instance.name;
#endif
        internal static System.Collections.IEnumerator RestartLoader() {
#if !BEAT_SABER_1_29_0
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
#if BEAT_SABER_1_29_0
            XRSettings.loadedDeviceName;
#else
            OpenXRRuntime.name;
#endif
        internal static string Version =>
#if BEAT_SABER_1_29_0
            string.Empty;
#else
            OpenXRRuntime.version;
#endif
        internal static bool EyeGazeEnabled =>
#if BEAT_SABER_1_29_0
            false;
#else
            OpenXRRuntime.IsExtensionEnabled(EyeGazeInteraction.extensionString);
#endif
        internal static void EnableEyeGaze() {
#if !BEAT_SABER_1_29_0
            var feature = OpenXRSettings.Instance?.GetFeature<EyeGazeInteraction>();
            if (feature != null) feature.enabled = true;
#endif
        }
        internal static IEnumerable<MethodBase> LoaderInitializationMethods {
            get {
#if !BEAT_SABER_1_29_0
                yield return typeof(OpenXRLoaderBase).GetMethod(nameof(OpenXRLoaderBase.Initialize));
#else
                yield break;
#endif
            }
        }
    }
}
