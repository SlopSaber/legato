#nullable enable

using UnityEngine;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace Legato.Rendering
{
    internal static class LegacyShaderSupport
    {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private static readonly ShaderTagId LightMode = new("LightMode");
        private static readonly ShaderTagId ForwardBase = new("ForwardBase");
        private static readonly ShaderTagId UniversalForward = new("UniversalForward");
        private static readonly ShaderTagId UniversalForwardOnly = new("UniversalForwardOnly");

        internal static bool IsUniversalPipeline() => GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;

        internal static bool HasForwardBase(Shader shader)
        {
            for (int subshader = 0; subshader < shader.subshaderCount; subshader++)
            {
                int count = shader.GetPassCountInSubshader(subshader);
                for (int pass = 0; pass < count; pass++)
                {
                    string mode = shader.FindPassTagValue(subshader, pass, LightMode).name;
                    if (string.Equals(mode, "ForwardBase", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool AppendForwardBase(List<ShaderTagId>? tags, ref DrawingSettings settings)
        {
            if (tags == null || tags.Count == 0 || tags.Count >= DrawingSettings.maxShaderPasses ||
                tags.Contains(ForwardBase) || (!tags.Contains(UniversalForward) && !tags.Contains(UniversalForwardOnly)))
            {
                return false;
            }

            // Keep the renderer's native priority, sorting and per-object data.
            settings.SetShaderPassName(tags.Count, ForwardBase);
            return true;
        }
#else
        internal static bool IsUniversalPipeline() => false;

        internal static bool HasForwardBase(Shader shader) => false;
#endif
    }
}
