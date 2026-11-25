using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
#if !LEGATO_BUILT_IN_RENDERING
using UnityEngine.Rendering.Universal;
#endif
#if LEGATO_MAIN_SETTINGS_HANDLER
using BeatSaber.GameSettings;
#endif

namespace Legato.Rendering {
    internal sealed class CameraOpaqueTexture {
#if !LEGATO_BUILT_IN_RENDERING
        private bool _overridden;
        private bool _original;
#endif
        internal void Update(Camera camera, bool replay, Action<bool> applied) {
#if !LEGATO_BUILT_IN_RENDERING
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
#if !LEGATO_BUILT_IN_RENDERING
            if (GraphicsSettings.currentRenderPipeline == null) return;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.allowXRRendering = false;
            data.cameraStack?.Clear();
#endif
        }
        internal static void SetDepthTexture(Camera camera) {
#if !LEGATO_BUILT_IN_RENDERING
            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                data.requiresDepthTexture = camera.depthTextureMode != DepthTextureMode.None;
#endif
        }
        internal static void Render(Camera camera, RenderTexture destination) {
#if !LEGATO_BUILT_IN_RENDERING
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
#if !LEGATO_BUILT_IN_RENDERING
            private readonly Action<ScriptableRenderContext, Camera> _camera;
            private readonly Action<ScriptableRenderContext, List<Camera>> _context;
            private bool _disposed;
#endif
            internal Subscription(Action<Camera> cameraEnded, Action contextEnded) {
#if !LEGATO_BUILT_IN_RENDERING
                _camera = (_, camera) => cameraEnded(camera);
                _context = (_, cameras) => contextEnded();
                RenderPipelineManager.endCameraRendering += _camera;
                RenderPipelineManager.endContextRendering += _context;
#endif
            }
            public void Dispose() {
#if !LEGATO_BUILT_IN_RENDERING
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
#elif LEGATO_MAIN_SETTINGS_HANDLER
            var settings = FieldAccessor<VRCenterAdjust, MainSettingsHandler>.Get(adjuster, "_mainSettingsHandler")?.instance;
            return settings == null ? (Vector3.zero, 0f) : (settings.roomCenter, settings.roomRotation);
#else
            var settings = FieldAccessor<VRCenterAdjust, SettingsManager>.Get(adjuster, "_settingsManager");
            return settings == null ? (Vector3.zero, 0f) : (settings.settings.room.center, settings.settings.room.rotation);
#endif
        }
        internal static MethodBase DepthInit =>
#if BEAT_SABER_1_29_0
            AccessTools.DeclaredMethod(typeof(MainSettingsModelSO), "Load");
#elif LEGATO_DEPTH_TEXTURE_AT_START
            AccessTools.DeclaredMethod(typeof(DepthTextureController), "Start");
#else
            AccessTools.DeclaredMethod(typeof(DepthTextureController), "Init");
#endif
        internal static bool DepthEnabled(object instance, object[] args) =>
#if BEAT_SABER_1_29_0
            ((MainSettingsModelSO)instance).depthTextureEnabled.value;
#elif LEGATO_DEPTH_TEXTURE_AT_START
            (((DepthTextureController)instance).GetComponent<Camera>().depthTextureMode & DepthTextureMode.Depth) != 0;
#else
            ((SettingsManager)args[0]).settings.quality.smokeGraphics;
#endif
        internal static MethodBase ObstacleProperties => AccessTools.DeclaredMethod(typeof(StretchableObstacle),
#if LEGATO_LEGACY_OBSTACLE_PROPERTIES
            "SetSizeAndColor");
#else
            "SetAllProperties");
#endif
    }
}
