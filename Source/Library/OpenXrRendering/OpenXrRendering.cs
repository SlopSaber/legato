#nullable enable

#if !BEAT_SABER_1_29_0
using UnityEngine.XR.OpenXR;
#endif

namespace Legato.XR {
    internal static class OpenXrRendering {
#if !BEAT_SABER_1_29_0
        internal static void SetRequestedMode(OpenXRSettings settings, bool multiPass) =>
            settings.m_renderMode = multiPass ? OpenXRSettings.RenderMode.MultiPass : OpenXRSettings.RenderMode.SinglePassInstanced;

        internal static void ApplyToCurrentInstance(bool multiPass) {
            if (OpenXRSettings.Instance != null && OpenXRLoaderBase.Instance != null)
                OpenXRSettings.Instance.renderMode = multiPass ? OpenXRSettings.RenderMode.MultiPass : OpenXRSettings.RenderMode.SinglePassInstanced;
        }
#else
        internal static void ApplyToCurrentInstance(bool multiPass) { }
#endif
    }
}
