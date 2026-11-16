#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Legato.Rendering
{
    // Built-in ForwardBase passes read the main directional light from these globals.
    internal class LegacyLightingPass : ScriptableRenderPass, System.IDisposable
    {
        private static readonly int LightColor = Shader.PropertyToID("_LightColor0");
        private static readonly int LightPosition = Shader.PropertyToID("_WorldSpaceLightPos0");

        private readonly string passName;
        private readonly bool restoreOnDispose;
        private readonly Vector4 originalColor;
        private readonly Vector4 originalPosition;
        private readonly bool originalDirectional;
        private bool disposed;

        internal LegacyLightingPass(string passName, bool restoreOnDispose)

        {
            this.passName = passName;
            this.restoreOnDispose = restoreOnDispose;
            if (restoreOnDispose)
            {
                originalColor = Shader.GetGlobalVector(LightColor);
                originalPosition = Shader.GetGlobalVector(LightPosition);
                originalDirectional = Shader.IsKeywordEnabled("DIRECTIONAL");
            }
            // Mirrors can render at BeforeRendering, before the ordinary opaque pass.
            renderPassEvent = RenderPassEvent.BeforeRendering;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (disposed) return;

            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(passName, out PassData data);
            data.Color = Vector4.zero;
            data.Position = new Vector4(0, 1, 0, 0);
            int mainLight = lightData.mainLightIndex;
            if (mainLight >= 0 && mainLight < lightData.visibleLights.Length)
            {
                VisibleLight light = lightData.visibleLights[mainLight];
                if (light.lightType == LightType.Directional)
                {
                    data.Color = light.finalColor;
                    Vector4 forward = light.localToWorldMatrix.GetColumn(2);
                    data.Position = new Vector4(-forward.x, -forward.y, -forward.z, 0);
                }
            }

            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalVector(LightColor, pass.Color);
                context.cmd.SetGlobalVector(LightPosition, pass.Position);
                context.cmd.EnableShaderKeyword("DIRECTIONAL");
            });
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (!restoreOnDispose) return;
            Shader.SetGlobalVector(LightColor, originalColor);
            Shader.SetGlobalVector(LightPosition, originalPosition);
            if (originalDirectional) Shader.EnableKeyword("DIRECTIONAL");
            else Shader.DisableKeyword("DIRECTIONAL");
        }

        private sealed class PassData
        {
            internal Vector4 Color { get; set; }

            internal Vector4 Position { get; set; }
        }
    }
}

#endif
