#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Legato.Rendering
{
    // URP cannot execute GrabPass. Shaders written for it sample _GrabTexture, which the
    // camera's opaque colour copy approximates (it lacks earlier transparent objects).
    internal class LegacyGrabTexturePass : ScriptableRenderPass
    {
        private static readonly int GrabTexture = Shader.PropertyToID("_GrabTexture");

        private readonly string passLabel;

        internal LegacyGrabTexturePass(string passName)
        {
            passLabel = passName;
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            TextureHandle opaque = resources.cameraOpaqueTexture;
            if (!opaque.IsValid())
            {
                return;
            }

            using IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass(passLabel, out PassData _);
            builder.UseTexture(opaque);
            builder.SetGlobalTextureAfterPass(opaque, GrabTexture);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData _, UnsafeGraphContext _) => { });
        }

        private sealed class PassData
        {
        }
    }
}

#endif
