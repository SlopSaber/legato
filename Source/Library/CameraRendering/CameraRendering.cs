using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
#if !BEAT_SABER_1_29_0
using UnityEngine.Rendering.Universal;
#endif

namespace Legato.Rendering {
    internal sealed class CameraOpaqueTexture {
#if !BEAT_SABER_1_29_0
        private bool _overridden;
        private bool _original;
#endif
        internal void Update(Camera camera, bool replay, Action<bool> applied) {
#if !BEAT_SABER_1_29_0
            if (!camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)) return;
            if (replay && !_overridden) {
                _original = data.requiresColorTexture;
                data.requiresColorTexture = true;
                _overridden = true;
                applied(data.requiresDepthTexture);
            } else if (!replay && _overridden) {
                data.requiresColorTexture = _original;
                _overridden = false;
            }
#endif
        }
    }
    internal static class CameraRendering {
        internal static void ConfigureDesktop(Camera camera) {
#if !BEAT_SABER_1_29_0
            if (GraphicsSettings.currentRenderPipeline == null) return;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.allowXRRendering = false;
            data.cameraStack?.Clear();
#endif
        }
        internal static void SetDepthTexture(Camera camera) {
#if !BEAT_SABER_1_29_0
            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                data.requiresDepthTexture = camera.depthTextureMode != DepthTextureMode.None;
#endif
        }
        internal static void Render(Camera camera, RenderTexture destination) {
#if !BEAT_SABER_1_29_0
            if (GraphicsSettings.currentRenderPipeline != null) {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = destination };
                if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                return;
            }
#endif
            camera.Render();
        }
        internal static IDisposable Subscribe(Action<Camera> cameraEnded, Action contextEnded) => new Subscription(cameraEnded, contextEnded);
        private sealed class Subscription : IDisposable {
#if !BEAT_SABER_1_29_0
            private readonly Action<ScriptableRenderContext, Camera> _camera;
            private readonly Action<ScriptableRenderContext, List<Camera>> _context;
            private bool _disposed;
#endif
            internal Subscription(Action<Camera> cameraEnded, Action contextEnded) {
#if !BEAT_SABER_1_29_0
                _camera = (_, camera) => cameraEnded(camera);
                _context = (_, cameras) => contextEnded();
                RenderPipelineManager.endCameraRendering += _camera;
                RenderPipelineManager.endContextRendering += _context;
#endif
            }
            public void Dispose() {
#if !BEAT_SABER_1_29_0
                if (_disposed) return;
                _disposed = true;
                RenderPipelineManager.endCameraRendering -= _camera;
                RenderPipelineManager.endContextRendering -= _context;
#endif
            }
        }
    }
    internal static class CameraNativeContext {
        internal static IEnumerable<MethodBase> RoomMethods => new [] {
            "OnEnable", "Start", "ResetRoom",
#if BEAT_SABER_1_29_0
            "HandleRoomCenterDidChange", "HandleRoomRotationDidChange"
#else
            "SetRoomTransformOffset"
#endif
        }.Select(name => (MethodBase)AccessTools.DeclaredMethod(typeof(VRCenterAdjust), name));
        internal static (Vector3 center, float rotation) Room(VRCenterAdjust adjuster) {
#if BEAT_SABER_1_29_0
            var settings = FieldAccessor<VRCenterAdjust, MainSettingsModelSO>.Get(adjuster, "_mainSettingsModel");
            return settings == null ? (Vector3.zero, 0f) : (settings.roomCenter.value, settings.roomRotation.value);
#else
            var settings = FieldAccessor<VRCenterAdjust, SettingsManager>.Get(adjuster, "_settingsManager");
            return settings == null ? (Vector3.zero, 0f) : (settings.settings.room.center, settings.settings.room.rotation);
#endif
        }
        internal static MethodBase DepthInit =>
#if BEAT_SABER_1_29_0
            AccessTools.DeclaredMethod(typeof(MainSettingsModelSO), "Load");
#else
            AccessTools.DeclaredMethod(typeof(DepthTextureController), "Init");
#endif
        internal static bool DepthEnabled(object instance, object[] args) =>
#if BEAT_SABER_1_29_0
            ((MainSettingsModelSO)instance).depthTextureEnabled.value;
#else
            ((SettingsManager)args[0]).settings.quality.smokeGraphics;
#endif
        internal static MethodBase ObstacleProperties => AccessTools.DeclaredMethod(typeof(StretchableObstacle),
#if BEAT_SABER_1_29_0
            "SetSizeAndColor");
#else
            "SetAllProperties");
#endif
    }
}
