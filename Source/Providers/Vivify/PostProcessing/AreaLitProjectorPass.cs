#if BEAT_SABER_1453
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Vivify.PostProcessing;

internal sealed class AreaLitProjector : MonoBehaviour
{
    private Projector _projector = null!;

    internal static HashSet<Projector> Active { get; } = [];

    internal static bool IsSupported(Projector projector) => projector.material != null && projector.material.shader != null &&
                                                           projector.material.shader.name == "AreaLit/Projector";

    private void Awake() => _projector = GetComponent<Projector>();

    private void OnEnable() => Active.Add(_projector);

    private void OnDisable() => Active.Remove(_projector);
}

internal sealed class AreaLitProjectorPass : ScriptableRenderPass
{
    private static readonly List<ShaderTagId> _receiverTags =
        [new("SRPDefaultUnlit"), new("UniversalForward"), new("UniversalForwardOnly"), new("UniversalGBuffer"), new("ForwardBase"), new("Always")];

    private readonly List<(CullingResults Receivers, Material Material, int LayerMask)> _projectors = [];
    private readonly Plane[] _planes = new Plane[6];
    private Camera? _camera;

    internal AreaLitProjectorPass() => renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

    internal bool HasReceivers => _projectors.Count > 0;

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        if (_camera != cameraData.camera || !HasReceivers)
        {
            return;
        }

        UniversalResourceData resources = frameData.Get<UniversalResourceData>();
        if (!resources.activeColorTexture.IsValid() || !resources.activeDepthTexture.IsValid())
        {
            return;
        }

        DrawingSettings settings = RenderingUtils.CreateDrawingSettings(
            _receiverTags,
            frameData.Get<UniversalRenderingData>(),
            cameraData,
            frameData.Get<UniversalLightData>(),
            cameraData.defaultOpaqueSortFlags);

        using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("AreaLit projectors", out PassData data);
        data.Lists.Clear();
        foreach ((CullingResults receivers, Material material, int layerMask) in _projectors)
        {
            // AreaLit evaluates light in world space. Only receiver selection uses the projector frustum.
            settings.overrideMaterial = material;
            settings.overrideMaterialPassIndex = 0;
            RendererListHandle list = renderGraph.CreateRendererList(new RendererListParams(
                receivers, settings, new FilteringSettings(RenderQueueRange.opaque, layerMask)));
            data.Lists.Add(list);
            builder.UseRendererList(list);
        }

        builder.SetRenderAttachment(resources.activeColorTexture, 0);
        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
        builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
        {
            foreach (RendererListHandle list in pass.Lists)
            {
                context.cmd.DrawRendererList(list);
            }
        });
    }

    internal void Cull(ScriptableRenderContext context, Camera camera, bool lightMeshCamera)
    {
        _camera = camera;
        _projectors.Clear();
        if (lightMeshCamera || AreaLitProjector.Active.Count == 0 || !camera.TryGetCullingParameters(false, out ScriptableCullingParameters parameters))
        {
            return;
        }

        parameters.cullingOptions &= ~(CullingOptions.OcclusionCull | CullingOptions.Stereo | CullingOptions.ShadowCasters);
        parameters.shadowDistance = 0;
        foreach (Projector projector in AreaLitProjector.Active)
        {
            if (projector == null || !projector.isActiveAndEnabled || !AreaLitProjector.IsSupported(projector))
            {
                continue;
            }

            int layerMask = camera.cullingMask & ~projector.ignoreLayers;
            if (layerMask == 0)
            {
                continue;
            }

            float height = projector.orthographicSize;
            float width = height * projector.aspectRatio;
            Matrix4x4 projection = projector.orthographic
                ? Matrix4x4.Ortho(-width, width, -height, height, projector.nearClipPlane, projector.farClipPlane)
                : Matrix4x4.Perspective(projector.fieldOfView, projector.aspectRatio, projector.nearClipPlane, projector.farClipPlane);
            Transform transform = projector.transform;
            Matrix4x4 view = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, -1)).inverse;
            Matrix4x4 frustum = projection * view;
            GeometryUtility.CalculateFrustumPlanes(frustum, _planes);
            parameters.cullingMatrix = frustum;
            parameters.cullingMask = unchecked((uint)layerMask);
            parameters.cullingPlaneCount = _planes.Length;
            for (int i = 0; i < _planes.Length; i++)
            {
                parameters.SetCullingPlane(i, _planes[i]);
            }

            _projectors.Add((context.Cull(ref parameters), projector.material, layerMask));
        }
    }

    private sealed class PassData
    {
        internal List<RendererListHandle> Lists { get; } = [];
    }
}
#endif
