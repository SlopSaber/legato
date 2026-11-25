#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Legato.Rendering
{
    // URP's opaque color copy approximates GrabPass before transparent objects draw.
    internal class LegacyGrabTexturePass : ScriptableRenderPass
    {
        private static readonly int GrabTexture = Shader.PropertyToID("_GrabTexture");

        private readonly string passLabel;
        private readonly int[] textureIds;
        private readonly int[] texelSizeIds;

        internal LegacyGrabTexturePass(string passName, IReadOnlyCollection<string>? namedTextures = null)
        {
            passLabel = passName;
            int count = namedTextures?.Count ?? 0;
            textureIds = new int[count + 1];
            texelSizeIds = new int[count];
            textureIds[0] = GrabTexture;
            if (namedTextures != null)
            {
                int index = 0;
                foreach (string name in namedTextures)
                {
                    textureIds[index + 1] = Shader.PropertyToID(name);
                    texelSizeIds[index++] = Shader.PropertyToID(name + "_TexelSize");
                }
            }

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

            using IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass(passLabel, out PassData data);
            builder.UseTexture(opaque);
            foreach (int id in textureIds)
            {
                builder.SetGlobalTextureAfterPass(opaque, id);
            }

            data.TexelSizeIds = texelSizeIds;
            if (texelSizeIds.Length > 0)
            {
                TextureDesc desc = renderGraph.GetTextureDesc(opaque);
                int width = Mathf.Max(1, desc.width);
                int height = Mathf.Max(1, desc.height);
                data.TexelSize = new Vector4(1f / width, 1f / height, width, height);
                builder.AllowGlobalStateModification(true);
            }

            builder.AllowPassCulling(false);
            builder.SetRenderFunc(static (PassData pass, UnsafeGraphContext context) =>
            {
                foreach (int texelSize in pass.TexelSizeIds)
                {
                    context.cmd.SetGlobalVector(texelSize, pass.TexelSize);
                }
            });
        }

        private sealed class PassData
        {
            internal int[] TexelSizeIds { get; set; } = System.Array.Empty<int>();

            internal Vector4 TexelSize { get; set; }
        }
    }
}

#endif
