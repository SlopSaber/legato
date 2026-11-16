#nullable enable

using UnityEngine.XR.OpenXR;

namespace Legato.XR {
    internal static class OpenXrRendering {
        internal static void SetRequestedMode(OpenXRSettings settings, bool multiPass) =>
            settings.m_renderMode = multiPass ? OpenXRSettings.RenderMode.MultiPass : OpenXRSettings.RenderMode.SinglePassInstanced;

        internal static void ApplyToCurrentInstance(bool multiPass) {
            if (OpenXRSettings.Instance != null && OpenXRLoaderBase.Instance != null)
                OpenXRSettings.Instance.renderMode = multiPass ? OpenXRSettings.RenderMode.MultiPass : OpenXRSettings.RenderMode.SinglePassInstanced;
        }
    }
}
