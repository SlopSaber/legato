#nullable enable

using System;
using HarmonyLib;
using UnityEngine;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace Legato.Rendering {
    internal static class MirrorCameraHooks {
        internal static Action<Camera, Camera>? LegacyMask { get; set; }
        internal static Func<Camera, bool, int>? ModernMask { get; set; }

#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private readonly struct MaskState {
            private readonly Camera _camera;
            private readonly int _originalMask;

            internal MaskState(Camera camera, bool stereo) {
                _camera = camera;
                _originalMask = camera == null ? 0 : camera.cullingMask;
                if (camera != null && ModernMask != null) camera.cullingMask = ModernMask(camera, stereo);
            }

            internal void Restore() {
                if (_camera != null) _camera.cullingMask = _originalMask;
            }
        }

        [HarmonyPatch(typeof(MirrorRendererFeature), nameof(MirrorRendererFeature.AddRenderPasses))]
        private static class FeatureHook {
            private static void Prefix(ref RenderingData renderingData, out MaskState __state) =>
                __state = new MaskState(renderingData.cameraData.camera, renderingData.cameraData.xr.enabled);

            private static void Finalizer(MaskState __state) => __state.Restore();
        }

        [HarmonyPatch(typeof(PlanarMirrorRenderPass), nameof(PlanarMirrorRenderPass.RecordRenderGraph))]
        private static class GraphHook {
            private static void Prefix(ContextContainer frameData, out MaskState __state) {
                UniversalCameraData camera = frameData.Get<UniversalCameraData>();
                __state = new MaskState(camera.camera, camera.xr.enabled);
            }

            private static void Finalizer(MaskState __state) => __state.Restore();
        }
#elif LEGATO_EARLY_STANDARD_MAPS
        [HarmonyPatch(typeof(MirrorRenderer), "CreateOrUpdateMirrorCamera")]
        private static class EarlyHook {
            [HarmonyPriority(300)]
            private static void Postfix(Camera __0, Camera ____mirrorCamera) => LegacyMask?.Invoke(____mirrorCamera, __0);
        }
#else
        [HarmonyPatch(typeof(MirrorRendererSO), "CreateOrUpdateMirrorCamera")]
        private static class LegacyHook {
            private static bool Prepare() => AccessTools.Method(typeof(MirrorRendererSO), "CreateOrUpdateMirrorCamera") != null;

            [HarmonyPriority(300)]
            private static void Postfix(Camera ____mirrorCamera) => LegacyMask?.Invoke(____mirrorCamera, Camera.current);
        }
#endif
    }
}
