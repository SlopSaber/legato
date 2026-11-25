#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if !LEGATO_BUILT_IN_RENDERING
using HarmonyLib;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
#else
using UnityEngine.XR;
#endif

namespace Legato.Rendering
{
    /// <summary>Publishes scene depth and an optional opaque-color material chain before transparent rendering.</summary>
    /// <remarks>Materials remain caller-owned. This does not capture transparent scenery or execute arbitrary renderer features.</remarks>
    internal sealed class CameraTexturePass : IDisposable
    {
        private static readonly List<CameraTexturePass> Registrations = new();
        private readonly Func<bool> active;
        private readonly int depthTexture;
        private readonly int depthAvailable;
        private readonly int colorTexture;
        private readonly Material[] materials;
        private readonly int downsample;
        private bool disposed;

        internal CameraTexturePass(Func<bool> active, string depthTexture, string depthAvailable,
            string? colorTexture = null, Material[]? materials = null, int downsample = 1)
        {
            this.active = active ?? throw new ArgumentNullException(nameof(active));
            if (downsample < 1) throw new ArgumentOutOfRangeException(nameof(downsample));
            if (string.IsNullOrEmpty(depthTexture) || string.IsNullOrEmpty(depthAvailable))
                throw new ArgumentException("Depth texture and availability property names are required.");
            this.depthTexture = Shader.PropertyToID(depthTexture);
            this.depthAvailable = Shader.PropertyToID(depthAvailable);
            this.materials = materials == null ? Array.Empty<Material>() : (Material[])materials.Clone();
            if (this.materials.Length != 0 && string.IsNullOrEmpty(colorTexture))
                throw new ArgumentException("A material chain requires an output texture property.");
            foreach (Material material in this.materials)
                if (material == null || material.shader == null || !material.shader.isSupported)
                    throw new ArgumentException("Every material must have a supported shader.");
            this.colorTexture = string.IsNullOrEmpty(colorTexture) ? 0 : Shader.PropertyToID(colorTexture);
            this.downsample = downsample;
            Registrations.Add(this);
#if !LEGATO_BUILT_IN_RENDERING
            pass = new GraphPass(this);
            reset.SetGlobalFloat(this.depthAvailable, 0);
            RenderPipelineManager.endCameraRendering += EndCamera;
#else
            Camera.onPreCull += PrepareCamera;
#endif
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Registrations.Remove(this);
            Shader.SetGlobalFloat(depthAvailable, 0);
#if !LEGATO_BUILT_IN_RENDERING
            RenderPipelineManager.endCameraRendering -= EndCamera;
            reset.Release();
#else
            Camera.onPreCull -= PrepareCamera;
            foreach (var pair in cameras) pair.Value.Dispose(pair.Key);
            cameras.Clear();
#endif
        }

#if !LEGATO_BUILT_IN_RENDERING
        private readonly GraphPass pass;
        private readonly CommandBuffer reset = new() { name = "Legato reset scene depth availability" };
        private void EndCamera(ScriptableRenderContext context, Camera camera) => context.ExecuteCommandBuffer(reset);

        [HarmonyPatch(typeof(ScriptableRenderer), "AddRenderPasses")]
        private static class RenderHook
        {
            private static void Prefix(ScriptableRenderer __instance, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.camera.cameraType != CameraType.Game) return;
                foreach (var registration in Registrations)
                    if (!registration.disposed && registration.active()) __instance.EnqueuePass(registration.pass);
            }
        }

        private sealed class GraphPass : ScriptableRenderPass
        {
            private readonly CameraTexturePass owner;
            internal GraphPass(CameraTexturePass owner)
            {
                this.owner = owner;
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = owner.materials.Length != 0;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>();
                bool available = resources.cameraDepthTexture.IsValid();
                using (var builder = graph.AddRasterRenderPass<DepthData>("Legato scene depth", out var data))
                {
                    data.Available = available;
                    if (available)
                    {
                        builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                        builder.SetGlobalTextureAfterPass(resources.cameraDepthTexture, owner.depthTexture);
                    }
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((DepthData value, RasterGraphContext context) =>
                        context.cmd.SetGlobalFloat(owner.depthAvailable, value.Available ? 1 : 0));
                }
                if (owner.materials.Length == 0) return;
                if (resources.isActiveTargetBackBuffer)
                    throw new InvalidOperationException("Legato camera material chains require an intermediate color target.");
                var source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.sizeMode = TextureSizeMode.Explicit;
                descriptor.width = Math.Max(1, camera.cameraTargetDescriptor.width / owner.downsample);
                descriptor.height = Math.Max(1, camera.cameraTargetDescriptor.height / owner.downsample);
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                descriptor.clearBuffer = false;
                descriptor.filterMode = FilterMode.Bilinear;
                for (int i = 0; i < owner.materials.Length; i++)
                {
                    descriptor.name = "Legato camera material " + i;
                    var destination = graph.CreateTexture(descriptor);
                    graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, owner.materials[i], 0), descriptor.name);
                    source = destination;
                }
                using (var builder = graph.AddRasterRenderPass<DepthData>("Legato publish camera texture", out _))
                {
                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(source, owner.colorTexture);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((DepthData _, RasterGraphContext context) => { });
                }
            }
            private sealed class DepthData { internal bool Available; }
        }
#else
        private readonly Dictionary<Camera, Buffers> cameras = new();
        private readonly List<Camera> removedCameras = new();

        private void PrepareCamera(Camera camera)
        {
            foreach (var entry in cameras) if (entry.Key == null) removedCameras.Add(entry.Key!);
            foreach (Camera removed in removedCameras) { cameras[removed].Dispose(removed); cameras.Remove(removed); }
            removedCameras.Clear();
            if (camera == null || camera.cameraType != CameraType.Game) return;
            if (GraphicsSettings.currentRenderPipeline != null)
                throw new NotSupportedException("This Legato built-in camera backend cannot execute in a different scriptable render pipeline.");
            if (disposed || !active())
            {
                if (cameras.TryGetValue(camera, out var old)) { old.Dispose(camera); cameras.Remove(camera); }
                return;
            }
            // DepthTextureMode is shared with other effects. Leave their additive depth request intact on disposal.
            camera.depthTextureMode |= DepthTextureMode.Depth;
            if (!cameras.TryGetValue(camera, out var buffers))
            {
                buffers = new Buffers(this);
                cameras.Add(camera, buffers);
                camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, buffers.Draw);
                camera.AddCommandBuffer(CameraEvent.AfterEverything, buffers.Release);
            }
            buffers.Record(camera, this);
        }

        private sealed class Buffers
        {
            internal readonly CommandBuffer Draw = new() { name = "Legato camera textures" };
            internal readonly CommandBuffer Release = new() { name = "Legato release camera textures" };
            private readonly int[] textures;
            private static int nextId;
            private bool recorded;
            private RenderTextureDescriptor previousDescriptor;
            internal Buffers(CameraTexturePass owner)
            {
                textures = new int[owner.materials.Length == 0 ? 0 : owner.materials.Length + 1];
                for (int i = 0; i < textures.Length; i++) textures[i] = Shader.PropertyToID("_LegatoCameraTarget" + (++nextId));
            }

            internal void Record(Camera camera, CameraTexturePass owner)
            {
                var descriptor = textures.Length == 0 ? default : camera.targetTexture != null ? camera.targetTexture.descriptor : camera.stereoEnabled
                    ? XRSettings.eyeTextureDesc : new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight,
                        camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, 0);
                if (recorded && (textures.Length == 0 || SameTarget(previousDescriptor, descriptor))) return;
                previousDescriptor = descriptor;
                recorded = true;
                Draw.Clear();
                Release.Clear();
                Draw.SetGlobalTexture(owner.depthTexture, new RenderTargetIdentifier("_CameraDepthTexture"));
                Draw.SetGlobalFloat(owner.depthAvailable, 1);
                Release.SetGlobalFloat(owner.depthAvailable, 0);
                if (textures.Length == 0) return;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.bindMS = false;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                descriptor.enableRandomWrite = false;
                descriptor.memoryless = RenderTextureMemoryless.None;
                Draw.GetTemporaryRT(textures[0], descriptor, FilterMode.Bilinear);
                if (descriptor.dimension == TextureDimension.Tex2DArray)
                {
                    for (int eye = 0; eye < descriptor.volumeDepth; eye++)
                        Draw.Blit(BuiltinRenderTextureType.CurrentActive, textures[0], eye, eye);
                }
                else Draw.Blit(BuiltinRenderTextureType.CurrentActive, textures[0]);
                int width = descriptor.width;
                int height = descriptor.height;
                descriptor.width = Math.Max(1, width / owner.downsample);
                descriptor.height = Math.Max(1, height / owner.downsample);
                for (int i = 0; i < owner.materials.Length; i++)
                {
                    Draw.GetTemporaryRT(textures[i + 1], descriptor, FilterMode.Bilinear);
                    Draw.SetGlobalTexture("_BlitTexture", textures[i]);
                    Draw.SetGlobalVector("_BlitTexture_TexelSize", new Vector4(1f / width, 1f / height, width, height));
                    Draw.SetGlobalVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                    Draw.SetRenderTarget(new RenderTargetIdentifier(textures[i + 1]), 0, CubemapFace.Unknown, -1);
                    Draw.DrawProcedural(Matrix4x4.identity, owner.materials[i], 0, MeshTopology.Triangles, 3,
                        descriptor.dimension == TextureDimension.Tex2DArray ? descriptor.volumeDepth : 1);
                    width = descriptor.width;
                    height = descriptor.height;
                }
                Draw.SetGlobalTexture(owner.colorTexture, textures[textures.Length - 1]);
                Draw.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                foreach (int texture in textures) Release.ReleaseTemporaryRT(texture);
            }

            private static bool SameTarget(RenderTextureDescriptor left, RenderTextureDescriptor right) =>
                left.width == right.width && left.height == right.height && left.volumeDepth == right.volumeDepth &&
                left.dimension == right.dimension && left.graphicsFormat == right.graphicsFormat &&
                left.vrUsage == right.vrUsage && left.useDynamicScale == right.useDynamicScale;

            internal void Dispose(Camera camera)
            {
                if (camera != null)
                {
                    camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, Draw);
                    camera.RemoveCommandBuffer(CameraEvent.AfterEverything, Release);
                }
                Draw.Release();
                Release.Release();
            }
        }
#endif
    }
}
