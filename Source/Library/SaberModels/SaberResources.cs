using System;
using System.Linq;
using UnityEngine;
using Zenject;
#if BEAT_SABER_1_29_0
using UnityEngine.SceneManagement;
#else
using static BGLib.UnityExtension.AddressablesExtensions;
#endif
using Object = UnityEngine.Object;

namespace Legato.Models;

internal sealed class SaberResourceCache
{
    private GameObject? model;
    private SaberTrailRenderer? renderer;

    internal SaberResourceCache() => SaberResources.ObserveAssets();

    internal bool TryGetModel(out GameObject prefab)
    {
        if (model == null && SaberResources.TryLoadModel(out var loaded)) model = loaded;
        prefab = model!;
        return prefab != null;
    }

    internal GameObject Model => model != null ? model : model = SaberResources.LoadModel();

    internal GameObject CreateDefaultModel(string name)
    {
#if BEAT_SABER_1_29_0
        if (!TryGetModel(out var prefab)) return NativeSaberFallback.CreateModel(name);
        return SaberResources.CreateModel(prefab, name);
#else
        return SaberResources.CreateModel(Model, name);
#endif
    }

    internal Material GetDefaultTrailMaterial(bool shared)
    {
#if BEAT_SABER_1_29_0
        if (!TryGetModel(out _)) return NativeSaberFallback.TrailMaterial;
#endif
        return SaberTrailAccess.GetMaterial(Renderer, shared);
    }

    internal SaberTrailRenderer CreateTrailRenderer(DiContainer container)
    {
#if BEAT_SABER_1_29_0
        if (!TryGetModel(out _)) return NativeSaberFallback.CreateTrailRenderer();
#endif
        return SaberResources.InstantiateRenderer(container, Renderer);
    }

    internal SaberTrailRenderer Renderer
    {
        get
        {
            if (renderer != null) return renderer;
            var loaded = SaberResources.LoadTrailRenderer();
            SaberTrailAccess.InitializeRenderer(loaded);
            return renderer = loaded;
        }
    }
}

internal static class SaberResources
{
#if BEAT_SABER_1_29_0
    private static GameObject? nativeModel;
    private static bool observingAssets;
    private static int lastNativeLookupFrame = -1;
#endif

    internal static void ObserveAssets()
    {
#if BEAT_SABER_1_29_0
        if (observingAssets) return;
        observingAssets = true;
        SceneManager.sceneLoaded += CaptureNativeModel;
#endif
    }

#if BEAT_SABER_1_29_0
    private static void CaptureNativeModel(Scene scene, LoadSceneMode mode)
    {
        lastNativeLookupFrame = -1;
        if (nativeModel == null) TryLoadModel(out _);
    }
#endif

    internal static SaberTrailRenderer LoadTrailRenderer() =>
#if BEAT_SABER_1_29_0
        LoadModel().GetComponent<SaberTrail>()._trailRendererPrefab;
#else
        LoadAsset<SaberTrailRenderer>("Assets/Prefabs/Effects/Sabers/SaberTrailRenderer.prefab");
#endif

    internal static GameObject LoadModel() =>
        TryLoadModel(out var model) ? model : throw ResourceException;

    internal static bool TryLoadModel(out GameObject model)
    {
#if BEAT_SABER_1_29_0
        // Hold the gameplay asset for later menu previews without loading another scene.
        if (nativeModel == null && lastNativeLookupFrame != Time.frameCount)
        {
            lastNativeLookupFrame = Time.frameCount;
            var candidate = Resources.FindObjectsOfTypeAll<SaberModelController>()
                .FirstOrDefault(controller => controller.name == "BasicSaberModel" && !controller.gameObject.scene.IsValid());
            if (candidate != null && candidate.GetComponent<SaberTrail>()?._trailRendererPrefab != null)
                nativeModel = candidate.gameObject;
        }
        model = nativeModel!;
#else
        model = LoadContent<GameObject>("Assets/Prefabs/Sabers/BasicSaberModel.prefab").FirstOrDefault()!;
#endif
        return model != null;
    }

    internal static SaberTrailRenderer InstantiateRenderer(DiContainer container, SaberTrailRenderer prefab) =>
#if BEAT_SABER_1_29_0
        container.InstantiatePrefabForComponent<SaberTrailRenderer>(prefab, Vector3.zero, Quaternion.identity, null);
#else
        container.InstantiatePrefabForComponentAt<SaberTrailRenderer>(prefab, Vector3.zero, Quaternion.identity, null);
#endif

    internal static GameObject CreateModel(GameObject prefab, string name)
    {
        var model = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        model.name = name;
        foreach (var glow in model.GetComponentsInChildren<SetSaberGlowColor>()) glow.enabled = false;
        foreach (var glow in model.GetComponentsInChildren<SetSaberFakeGlowColor>()) glow.enabled = false;
        model.GetComponent<SaberTrail>().enabled = false;
        return model;
    }

    internal static void SetFallbackModelColor(GameObject model, Color color)
    {
#if BEAT_SABER_1_29_0
        if (model.TryGetComponent<NativeSaberFallbackColor>(out var fallback)) fallback.SetColor(color);
#endif
    }

#if !BEAT_SABER_1_29_0
    private static T LoadAsset<T>(object label) where T : Object =>
        LoadContent<GameObject>(label).FirstOrDefault()?.GetComponent<T>() ?? throw ResourceException;

#endif

    private static Exception ResourceException => new InvalidOperationException("An internal resource failed to load");
}
