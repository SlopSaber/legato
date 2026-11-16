#nullable enable
using System;
using System.Reflection;
using UnityEngine;
#if BEAT_SABER_1_45_3
using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
#endif

namespace Legato.Rendering {
    internal static class SaberDepthOcclusion {
        internal static void Attach(Camera camera, Func<bool> eligible, int saberMask, int displacementLayer, string label, Action applied) {
#if BEAT_SABER_1_45_3
            camera.gameObject.AddComponent<Registration>().Initialize(camera, eligible, saberMask, displacementLayer, label, applied);
#endif
        }

        internal static Shader LoadPreviewShader(Assembly assembly, string resource, string asset, Shader fallback, string label, Action<string> warn) {
#if BEAT_SABER_1_45_3
            try {
                using (var stream = assembly.GetManifestResourceStream(resource)) {
                    var bundle = stream == null ? null : AssetBundle.LoadFromStream(stream);
                    if (bundle == null) {
                        warn($"{label} bundle could not be loaded; retaining the legacy preview shader.");
                        return fallback;
                    }
                    try {
                        var shader = bundle.LoadAsset<Shader>(asset);
                        if (shader != null && shader.isSupported) return shader;
                        warn($"{label} shader is unavailable; retaining the legacy preview shader.");
                    } finally {
                        bundle.Unload(false);
                    }
                }
            } catch (Exception exception) {
                warn($"{label} shader could not be loaded; retaining the legacy preview shader: {exception}");
            }
#endif
            return fallback;
        }

#if BEAT_SABER_1_45_3
	private sealed class Registration : MonoBehaviour {
		private Camera ownerCamera = null!;
		private Func<bool> eligible = null!;
		private Action applied = null!;
		private int saberMask;
		private int displacementLayer;
		private string label = null!;
		private RenderObjects feature = null!;
		private int layerMask;
		private RenderPassEvent passEvent;
		private SceneDepthPass sceneDepth = null!;
		private SaberPass sabers = null!;
		private RenderObjectsPass others = null!;
		private bool logged;

		internal void Initialize(Camera owner, Func<bool> isEligible, int mask, int screenDisplacementLayer, string passLabel, Action onApplied) {
			ownerCamera = owner;
			eligible = isEligible;
			saberMask = mask;
			displacementLayer = screenDisplacementLayer;
			label = passLabel;
			applied = onApplied;
		}

		internal bool Enqueue(RenderObjects source, ScriptableRenderer renderer) {
			var settings = source.settings;
			if(ownerCamera == null || !eligible() || !ScreenDisplacementEffectRendererFeature.enabled ||
				displacementLayer < 0 ||
				(ownerCamera.cullingMask & (1 << displacementLayer)) == 0 ||
				(SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) == 0 ||
				settings.passTag != "TransparentAfterScreenDisplacement" ||
				settings.filterSettings.RenderQueueType != RenderQueueType.Transparent ||
				(settings.filterSettings.LayerMask.value & saberMask) == 0 ||
				settings.Event != RenderPassEvent.BeforeRenderingPostProcessing ||
				settings.overrideMaterial != null || settings.overrideShader != null || settings.overrideDepthState ||
				settings.stencilSettings.overrideStencilState || settings.cameraSettings.overrideCamera ||
				(settings.filterSettings.PassNames != null && settings.filterSettings.PassNames.Length != 0))
				return false;

			if(feature != source || layerMask != settings.filterSettings.LayerMask.value || passEvent != settings.Event) {
				feature = source;
				layerMask = settings.filterSettings.LayerMask.value;
				passEvent = settings.Event;
				sceneDepth = new SceneDepthPass(label);
				sabers = new SaberPass(sceneDepth, settings, saberMask, label);
				others = new RenderObjectsPass(settings.passTag, settings.Event, null,
					RenderQueueType.Transparent, layerMask & ~saberMask, settings.cameraSettings);
			}
			renderer.EnqueuePass(sceneDepth);
			renderer.EnqueuePass(sabers);
			if((layerMask & ~saberMask) != 0) renderer.EnqueuePass(others);
			if(!logged) {
				applied();
				logged = true;
			}
			return true;
		}

		private sealed class SceneDepthPass : ScriptableRenderPass {
			private sealed class PassData {
				internal TextureHandle source;
				internal TextureHandle destination;
			}

			private readonly string label;

			internal TextureHandle depth;

			internal SceneDepthPass(string passLabel) {
				label = passLabel;
				renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing - 1);
			}

			public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData) {
				depth = TextureHandle.nullHandle;
				var resources = frameData.Get<UniversalResourceData>();
				if(resources.isActiveTargetBackBuffer || !resources.activeDepthTexture.IsValid() ||
					resources.activeDepthTexture != resources.cameraDepth) return;

				var descriptor = graph.GetTextureDesc(resources.activeDepthTexture);
				descriptor.name = $"{label} depth before walls";
				descriptor.clearBuffer = false;
				descriptor.memoryless = RenderTextureMemoryless.None;
				depth = graph.CreateTexture(descriptor);
				using(var builder = graph.AddUnsafePass<PassData>($"{label} save depth before walls", out var data)) {
					data.source = resources.activeDepthTexture;
					data.destination = depth;
					builder.UseTexture(data.source, AccessFlags.Read);
					builder.UseTexture(data.destination, AccessFlags.WriteAll);
					builder.SetRenderFunc((PassData pass, UnsafeGraphContext context) => {
						// Copy the entire matching depth/stencil surface, including every MSAA sample.
						CommandBufferHelpers.GetNativeCommandBuffer(context.cmd).CopyTexture(
							(RenderTargetIdentifier)pass.source, (RenderTargetIdentifier)pass.destination);
					});
				}
			}
		}

		private sealed class SaberPass : RenderObjectsPass {
			private readonly SceneDepthPass sceneDepth;

			internal SaberPass(SceneDepthPass depth, RenderObjects.RenderObjectsSettings settings, int saberMask, string label)
				: base($"{label} sabers through walls", settings.Event, null, RenderQueueType.Transparent,
					saberMask, settings.cameraSettings) {
				sceneDepth = depth;
			}

			public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData) {
				var resources = frameData.Get<UniversalResourceData>();
				var original = resources.cameraDepth;
				if(!sceneDepth.depth.IsValid() || resources.activeDepthTexture != original) {
					base.RecordRenderGraph(graph, frameData);
					return;
				}
				// Only this renderer list binds the saved depth; subsequent passes retain the camera's depth.
				resources.cameraDepth = sceneDepth.depth;
				try {
					base.RecordRenderGraph(graph, frameData);
				} finally {
					resources.cameraDepth = original;
				}
			}
		}
	}

	[HarmonyPatch(typeof(RenderObjects), nameof(RenderObjects.AddRenderPasses))]
	private static class RenderObjectsHook {
		static bool Prefix(RenderObjects __instance, ScriptableRenderer renderer, ref RenderingData renderingData) {
			var camera = renderingData.cameraData.camera;
			return camera == null || !camera.TryGetComponent<Registration>(out var pass) || !pass.Enqueue(__instance, renderer);
		}
	}
#endif
    }
}
