#nullable enable

using System;
using UnityEngine;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
#endif

namespace Legato.Rendering {
    internal static class CameraMaterialEffects {
        internal static IDisposable? Register(GameObject owner, Func<Material> material, Func<bool> active, Action prepare) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            var registration = new Registration(owner, material, active, prepare);
            if (!Effects.TryGetValue(owner, out var entries)) Effects[owner] = entries = new List<Registration>();
            entries.Add(registration);
            return registration;
#else
            return null;
#endif
        }

#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private static readonly Dictionary<GameObject, List<Registration>> Effects = new();

        private sealed class Registration : IDisposable {
            internal readonly GameObject Owner;
            internal readonly Func<Material> Material;
            internal readonly Func<bool> Active;
            internal readonly Action Prepare;
            internal readonly MaterialRenderPass Pass = new();

            internal Registration(GameObject owner, Func<Material> material, Func<bool> active, Action prepare) {
                Owner = owner;
                Material = material;
                Active = active;
                Prepare = prepare;
            }

            public void Dispose() {
                if (!Effects.TryGetValue(Owner, out var entries)) return;
                entries.Remove(this);
                if (entries.Count == 0) Effects.Remove(Owner);
            }
        }

        [HarmonyPatch(typeof(ScriptableRenderer), "AddRenderPasses")]
        private static class RenderHook {
            private static void Postfix(ScriptableRenderer __instance, ref RenderingData renderingData) {
                Camera camera = renderingData.cameraData.camera;
                if (camera == null || !Effects.TryGetValue(camera.gameObject, out var entries)) return;
                Registration effect = entries[0];
                if (!effect.Active()) return;
                Material material = effect.Material();
                if (material == null || material.shader == null || !material.shader.isSupported) return;
                effect.Prepare();
                effect.Pass.Material = material;
                __instance.EnqueuePass(effect.Pass);
            }
        }

	private sealed class MaterialRenderPass : ScriptableRenderPass
	{
		private sealed class PassData
		{
			internal Material Material = null!;
			internal Mesh FullscreenMesh = null!;
			internal TextureHandle MainTexture;
			internal Matrix4x4 OriginalView;
			internal Matrix4x4 OriginalProjection;
			internal readonly Matrix4x4[] OriginalStereoViewProjection = new Matrix4x4[2];
			internal bool Stereo;
		}

		private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
		private static readonly Matrix4x4[] IdentityStereoMatrices = { Matrix4x4.identity, Matrix4x4.identity };
		private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
		private static readonly int StereoViewProjectionId = Shader.PropertyToID("unity_StereoMatrixVP");
		private readonly Mesh _fullscreenMesh;

		internal Material Material = null!;

		internal MaterialRenderPass()
		{
			renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
			requiresIntermediateTexture = true;
#pragma warning disable CS0618 // The original shader reads mesh position and UV attributes.
			_fullscreenMesh = RenderingUtils.fullscreenMesh;
#pragma warning restore CS0618
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (Material == null)
			{
				return;
			}
			UniversalResourceData resources = frameData.Get<UniversalResourceData>();
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			TextureHandle color = resources.activeColorTexture;
			TextureDesc copyDescription = renderGraph.GetTextureDesc(color);
			copyDescription.name = "CameraMaterialEffectSource";
			copyDescription.clearBuffer = false;
			TextureHandle copy = renderGraph.CreateTexture(copyDescription);
			renderGraph.AddBlitPass(color, copy, Vector2.one, Vector2.zero, passName: "Camera material effect copy");

			using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("Camera material effect", out PassData passData);
			passData.Material = Material;
			passData.FullscreenMesh = _fullscreenMesh;
			passData.MainTexture = copy;
			passData.OriginalView = cameraData.GetViewMatrix();
			passData.OriginalProjection = cameraData.GetProjectionMatrix();
			passData.Stereo = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;
			if (passData.Stereo)
			{
				// The pass uses an intermediate color texture, matching URP's XR projection setup.
				for (int eye = 0; eye < 2; eye++)
				{
					passData.OriginalStereoViewProjection[eye] =
						GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(eye), true) * cameraData.GetViewMatrix(eye);
				}
			}
			builder.UseTexture(copy);
			builder.SetRenderAttachment(color, 0);
			builder.AllowGlobalStateModification(true);
			builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
			{
				PropertyBlock.Clear();
				PropertyBlock.SetTexture(MainTextureId, data.MainTexture);
				// The bundled shader multiplies mesh vertices by camera matrices, including per-eye VP.
				context.cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
				if (data.Stereo)
				{
					context.cmd.SetGlobalMatrixArray(StereoViewProjectionId, IdentityStereoMatrices);
				}
				context.cmd.DrawMesh(data.FullscreenMesh, Matrix4x4.identity, data.Material, 0, 0, PropertyBlock);
				if (data.Stereo)
				{
					context.cmd.SetGlobalMatrixArray(StereoViewProjectionId, data.OriginalStereoViewProjection);
				}
				context.cmd.SetViewProjectionMatrices(data.OriginalView, data.OriginalProjection);
			});
		}
	}
#endif
    }
}
