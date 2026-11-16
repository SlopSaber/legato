#if BEAT_SABER_1453
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Vivify.PostProcessing;

internal sealed class UrpPostProcessingPass : ScriptableRenderPass
{
    private static readonly int _mainTex = Shader.PropertyToID("_MainTex");
    private static readonly int _mainTexSize = Shader.PropertyToID("_MainTex_TexelSize");
    private static readonly int _stereoViewProjection = Shader.PropertyToID("unity_StereoMatrixVP");
    private static readonly Matrix4x4[] _identityStereo = [Matrix4x4.identity, Matrix4x4.identity];

    private readonly List<MaterialData> _effects;
    private readonly Mesh _mesh;

    internal UrpPostProcessingPass(List<MaterialData> effects, RenderPassEvent passEvent)
    {
        _effects = effects;
        renderPassEvent = passEvent;
        requiresIntermediateTexture = true;
#pragma warning disable CS0618 // Bundled image-effect shaders consume mesh position and UV attributes.
        _mesh = RenderingUtils.fullscreenMesh;
#pragma warning restore CS0618
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resources = frameData.Get<UniversalResourceData>();
        if (_effects.Count == 0 || resources.isActiveTargetBackBuffer)
        {
            return;
        }

        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        TextureHandle color = resources.activeColorTexture;
        bool changed = false;
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            MaterialData effect = _effects[i];
            if (effect.Frame != null && effect.Frame != Time.frameCount)
            {
                _effects.RemoveAt(i);
                continue;
            }

            Material? material = effect.Material;
            if (material == null || effect.Source != VivifyController.CAMERA_TARGET)
            {
                continue;
            }

            foreach (string target in effect.Targets)
            {
                if (target != VivifyController.CAMERA_TARGET)
                {
                    continue;
                }

                TextureDesc descriptor = renderGraph.GetTextureDesc(color);
                descriptor.name = "Vivify screen effect";
                descriptor.bindTextureMS = false;
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(descriptor);

                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Vivify screen effect", out PassData data);
                data.Material = material;
                data.Pass = effect.Pass;
                data.Source = color;
                data.Mesh = _mesh;
                data.TexelSize = new Vector4(1f / descriptor.width, 1f / descriptor.height, descriptor.width, descriptor.height);
                data.View = cameraData.GetViewMatrix();
                data.Projection = cameraData.GetProjectionMatrix();
                data.RestoreStereo = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;
                if (data.RestoreStereo)
                {
                    for (int eye = 0; eye < 2; eye++)
                    {
                        data.StereoViewProjection[eye] =
                            GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(eye), true) * cameraData.GetViewMatrix(eye);
                    }
                }

                builder.UseTexture(color);
                builder.SetRenderAttachment(destination, 0);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
                {
                    MaterialPropertyBlock properties = pass.Properties;
                    properties.Clear();
                    properties.SetTexture(_mainTex, pass.Source);
                    properties.SetVector(_mainTexSize, pass.TexelSize);
                    context.cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    try
                    {
                        if (pass.RestoreStereo)
                        {
                            context.cmd.SetGlobalMatrixArray(_stereoViewProjection, _identityStereo);
                        }

                        context.cmd.DrawMesh(pass.Mesh, Matrix4x4.identity, pass.Material, 0, pass.Pass, properties);
                    }
                    finally
                    {
                        if (pass.RestoreStereo)
                        {
                            context.cmd.SetGlobalMatrixArray(_stereoViewProjection, pass.StereoViewProjection);
                        }

                        context.cmd.SetViewProjectionMatrices(pass.View, pass.Projection);
                    }
                });
                color = destination;
                changed = true;
            }
        }

        if (changed)
        {
            // Later pipeline passes consume the result directly, avoiding a copy back to the source.
            resources.cameraColor = color;
        }
    }

    private sealed class PassData
    {
        internal MaterialPropertyBlock Properties { get; } = new();

        internal Matrix4x4[] StereoViewProjection { get; } = new Matrix4x4[2];

        internal Material Material { get; set; } = null!;

        internal Mesh Mesh { get; set; } = null!;

        internal TextureHandle Source { get; set; }

        internal Vector4 TexelSize { get; set; }

        internal Matrix4x4 View { get; set; }

        internal Matrix4x4 Projection { get; set; }

        internal int Pass { get; set; }

        internal bool RestoreStereo { get; set; }
    }
}
#endif
