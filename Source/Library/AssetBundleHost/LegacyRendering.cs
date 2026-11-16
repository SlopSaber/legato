#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Legato.Rendering;

namespace AssetBundleLoadingTools.Rendering
{
    /// <summary>
    /// Lets assets built for the built-in render pipeline draw under URP.
    /// </summary>
    /// <remarks>
    /// Registered content enables a ForwardBase fallback in URP's draw lists, together with the legacy
    /// main-light globals those passes read. GrabPass is emulated with the camera opaque texture once
    /// Unity reports one being drawn.
    /// </remarks>
    public static class LegacyRendering
    {
        private static readonly HashSet<Shader> CheckedShaders = new();
        private static readonly List<Material> MaterialBuffer = new();

        private static volatile bool grabPassRequested;
        private static bool listening;

        internal static bool ForwardBaseEnabled { get; private set; }

        internal static bool GrabPassEnabled => grabPassRequested;

        internal static LegacyLightingPass? LightingPass { get; private set; }

        internal static LegacyGrabTexturePass? GrabTexturePass { get; private set; }

        /// <summary>
        /// Enables legacy rendering if any renderer under <paramref name="root"/> uses a built-in pipeline shader.
        /// </summary>
        /// <param name="root">The loaded prefab or instance to inspect.</param>
        /// <returns><see langword="true"/> if the object uses a ForwardBase shader.</returns>
        public static bool Register(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.GetSharedMaterials(MaterialBuffer);
                foreach (Material material in MaterialBuffer)
                {
                    found |= material != null && Register(material.shader);
                }
            }

            MaterialBuffer.Clear();
            return found;
        }

        /// <summary>
        /// Enables legacy rendering if <paramref name="shader"/> only has built-in pipeline lit passes.
        /// </summary>
        /// <param name="shader">The shader to inspect.</param>
        /// <returns><see langword="true"/> if the shader has a ForwardBase pass.</returns>
        public static bool Register(Shader shader)
        {
            if (shader == null || !IsUniversalPipeline())
            {
                return false;
            }

            if (!CheckedShaders.Add(shader))
            {
                return HasForwardBase(shader);
            }

            if (!HasForwardBase(shader))
            {
                return false;
            }

            if (!ForwardBaseEnabled)
            {
                ForwardBaseEnabled = true;
                LightingPass = new LegacyLightingPass("Legacy lighting", false);
                Plugin.Log.Info($"Enabled legacy ForwardBase rendering (first shader: {shader.name}).");
            }

            return true;
        }

        internal static void StartListening()
        {
            if (listening)
            {
                return;
            }

            listening = true;
            GrabTexturePass = new LegacyGrabTexturePass("Legacy grab texture");

            // Unity reports a GrabPass only when one is drawn, from a render job thread.
            Application.logMessageReceivedThreaded += OnLogMessage;
        }

        internal static void StopListening()
        {
            if (listening)
            {
                listening = false;
                Application.logMessageReceivedThreaded -= OnLogMessage;
            }
        }

        private static bool IsUniversalPipeline() => LegacyShaderSupport.IsUniversalPipeline();

        private static bool HasForwardBase(Shader shader) => LegacyShaderSupport.HasForwardBase(shader);

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!grabPassRequested && type == LogType.Error && condition.StartsWith("ShaderLab::GrabPasses", StringComparison.Ordinal))
            {
                grabPassRequested = true;
                Plugin.Log.Info("A loaded shader uses GrabPass; providing the camera opaque texture as _GrabTexture.");
            }
        }
    }
}

#else
using UnityEngine;

namespace AssetBundleLoadingTools.Rendering
{
    public static class LegacyRendering
    {
        public static bool Register(GameObject root) => false;
        public static bool Register(Shader shader) => false;
        internal static void StartListening() { }
        internal static void StopListening() { }
    }
}
#endif
