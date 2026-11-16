#if BEAT_SABER_1453
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches;

internal sealed class UrpCameraEffects : IAffinity, IDisposable
{
    private static readonly (PostProcessingOrder Order, RenderPassEvent Event)[] _orders =
    [
        (PostProcessingOrder.BeforeOpaque, RenderPassEvent.BeforeRenderingOpaques),
        (PostProcessingOrder.AfterOpaque, RenderPassEvent.AfterRenderingOpaques),
        (PostProcessingOrder.BeforeSkybox, RenderPassEvent.BeforeRenderingSkybox),
        (PostProcessingOrder.AfterSkybox, RenderPassEvent.AfterRenderingSkybox),
        (PostProcessingOrder.BeforeAlpha, RenderPassEvent.BeforeRenderingTransparents),
        (PostProcessingOrder.AfterAlpha, RenderPassEvent.AfterRenderingTransparents),
        (PostProcessingOrder.BeforeMainEffect, (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing - 1)),
        (PostProcessingOrder.AfterMainEffect, (RenderPassEvent)((int)RenderPassEvent.AfterRenderingPostProcessing + 1)),
    ];

    private readonly CameraEffectApplier _effects;
    private readonly SiraLog _log;
    private readonly Dictionary<Camera, UrpPostProcessingPass[]> _cameras = new();

    [UsedImplicitly]
    private UrpCameraEffects(CameraEffectApplier effects, SiraLog log)
    {
        _effects = effects;
        _log = log;
    }

    public void Dispose()
    {
        _cameras.Clear();
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(ScriptableRenderer), "AddRenderPasses")]
    private void QueueEffects(ScriptableRenderer __instance, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        if (camera.cameraType != CameraType.Game || renderingData.cameraData.renderType != CameraRenderType.Base ||
            !Camera2PriorityScene.ShouldRenderEffects(camera) || camera.TryGetComponent(out SecondaryCameraController _))
        {
            return;
        }

        bool active = false;
        foreach (List<MaterialData> effects in _effects.Effects.Values)
        {
            active |= effects.Count != 0;
        }

        if (!active)
        {
            return;
        }

        if (!_cameras.TryGetValue(camera, out UrpPostProcessingPass[] passes))
        {
            passes = new UrpPostProcessingPass[_orders.Length];
            for (int i = 0; i < _orders.Length; i++)
            {
                (PostProcessingOrder order, RenderPassEvent passEvent) = _orders[i];
                passes[i] = new UrpPostProcessingPass(_effects.Effects[order], passEvent);
            }

            _cameras.Add(camera, passes);
            _log.Info($"Enabled URP screen effects for [{camera.name}].");
        }

        for (int i = 0; i < _orders.Length; i++)
        {
            if (_effects.Effects[_orders[i].Order].Count != 0)
            {
                __instance.EnqueuePass(passes[i]);
            }
        }
    }
}
#endif
