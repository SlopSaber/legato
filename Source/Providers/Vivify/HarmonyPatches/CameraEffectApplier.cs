using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck.ReLoad;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using UnityEngine;
using Vivify.PostProcessing;
using Zenject;

namespace Vivify.HarmonyPatches;

internal class CameraEffectApplier : IAffinity, IDisposable
{
#if !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
    private readonly HashSet<PostProcessingController> _nativeControllers = new();
#else
    private readonly Dictionary<MainEffectController, PostProcessingController> _postProcessingControllers = new();
#endif

    private readonly ReLoader? _reLoader;
    private readonly int _prewarmCount;

    [UsedImplicitly]
    private CameraEffectApplier(
        IReadonlyBeatmapData beatmapData,
        [InjectOptional] ReLoader? reLoader)
    {
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += OnRewind;
        }

        // TODO: correctly count concurrent created cameras
        _prewarmCount = ((CustomBeatmapData)beatmapData).customEventDatas.Any(
            n => n.eventType == VivifyController.DECLARE_CULLING_TEXTURE)
            ? 1
            : 0;

        foreach (PostProcessingOrder key in Enum.GetValues(typeof(PostProcessingOrder)))
        {
            if (!Effects.ContainsKey(key))
            {
                Effects[key] = [];
            }
        }
    }

    internal Dictionary<string, CreateCameraData> CameraDatas { get; } = new();

    internal Dictionary<string, CreateScreenTextureData> DeclaredTextureDatas { get; } = new();

    internal Dictionary<PostProcessingOrder, List<MaterialData>> Effects { get; } = new();

    public void Dispose()
    {
        Reset();

        if (_reLoader != null)
        {
            _reLoader.Rewinded -= OnRewind;
        }
    }

    private void OnRewind()
    {
        Reset();
    }

    private void Reset()
    {
        CameraDatas.Clear();
        DeclaredTextureDatas.Clear();

        foreach (List<MaterialData>? materials in Effects.Values)
        {
            materials.Clear();
        }
    }

#if !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
    [AffinityPrefix]
    [AffinityPatch(typeof(PostProcessingController), "OnPreCull")]
    private void BindNativeCamera(PostProcessingController __instance)
    {
        if (_nativeControllers.Add(__instance))
        {
            AttachEffects(__instance);
        }
    }
#else
    [AffinityPrefix]
    [AffinityPatch(typeof(MainEffectController), "get_renderData")]
    private void ApplyVivifyEffect(MainEffectController __instance)
    {
        if (_postProcessingControllers.TryGetValue(__instance, out PostProcessingController? postProcessingController))
        {
            return;
        }

        postProcessingController = __instance.GetComponent<PostProcessingController?>();
        if (postProcessingController == null)
        {
            return;
        }

        _postProcessingControllers[__instance] = postProcessingController;
        AttachEffects(postProcessingController);
    }
#endif

    private void AttachEffects(PostProcessingController postProcessingController)
    {
        postProcessingController.CameraDatas = CameraDatas;
        postProcessingController.DeclaredTextureDatas = DeclaredTextureDatas;
        postProcessingController.Effects = Effects;
        postProcessingController.PrewarmCameras(_prewarmCount);
    }
}
