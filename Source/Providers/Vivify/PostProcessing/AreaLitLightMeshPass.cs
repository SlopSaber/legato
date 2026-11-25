#if BEAT_SABER_1453
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Vivify.PostProcessing;

internal sealed class AreaLitLightMeshPass : ScriptableRenderPass
{
    internal static readonly int LightMeshProperty = Shader.PropertyToID("_LightMesh");
    internal static readonly ShaderTagId UniversalForward = new("UniversalForward");
    internal static readonly ShaderTagId SkipForward = new("LegatoAreaLitSkipForward");

    private static readonly ShaderTagId _lightMode = new("LightMode");
    private static readonly ShaderTagId _vertex = new("Vertex");

    private readonly HashSet<RenderTexture> _targets;
    private readonly List<(Shader Shader, int Queue, int Pass)> _passes = [];

    internal AreaLitLightMeshPass(Material clear, Material lights, HashSet<RenderTexture> targets)
    {
        _targets = targets;
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        AddVertexPasses(clear);
        int clearPasses = _passes.Count;
        AddVertexPasses(lights);
        if (clearPasses == 0 || _passes.Count == clearPasses)
        {
            _passes.Clear();
        }
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        if (!Matches(cameraData.camera))
        {
            return;
        }

        UniversalResourceData resources = frameData.Get<UniversalResourceData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        DrawingSettings settings = RenderingUtils.CreateDrawingSettings(
            _vertex, renderingData, cameraData, frameData.Get<UniversalLightData>(), SortingCriteria.CommonTransparent);

        using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("AreaLit light mesh", out PassData data);
        data.Lists.Clear();
        foreach ((Shader shader, int queue, int pass) in _passes)
        {
            settings.overrideShader = shader;
            settings.overrideShaderPassIndex = pass;
            RenderQueueRange range = new(queue, queue);
            RendererListHandle list = renderGraph.CreateRendererList(new RendererListParams(
                renderingData.cullResults, settings, new FilteringSettings(range, cameraData.camera.cullingMask)));
            data.Lists.Add(list);
            builder.UseRendererList(list);
        }

        builder.SetRenderAttachment(resources.activeColorTexture, 0);
        builder.SetRenderAttachmentDepth(resources.activeDepthTexture);
        builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
        {
            foreach (RendererListHandle list in pass.Lists)
            {
                context.cmd.DrawRendererList(list);
            }
        });
    }

    internal bool Matches(Camera camera) => _passes.Count > 0 && camera.renderingPath == RenderingPath.VertexLit &&
                                            camera.targetTexture != null && _targets.Contains(camera.targetTexture);

    private void AddVertexPasses(Material material)
    {
        Shader shader = material.shader;
        if (shader.subshaderCount == 0)
        {
            return;
        }

        for (int pass = 0; pass < shader.GetPassCountInSubshader(0); pass++)
        {
            if (string.Equals(shader.FindPassTagValue(0, pass, _lightMode).name, _vertex.name, StringComparison.OrdinalIgnoreCase))
            {
                _passes.Add((shader, material.renderQueue, pass));
            }
        }
    }

    private sealed class PassData
    {
        internal List<RendererListHandle> Lists { get; } = [];
    }
}
#endif
