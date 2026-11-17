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
        if (nativeModel == null)
        {
            nativeModel = Resources.FindObjectsOfTypeAll<SaberModelController>()
                .Where(candidate => candidate.name == "BasicSaberModel" || candidate.name == "BasicSaberModel(Clone)")
                .OrderBy(candidate => candidate.gameObject.scene.IsValid())
                .Select(candidate => candidate.gameObject).FirstOrDefault();
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

#if !BEAT_SABER_1_29_0
    private static T LoadAsset<T>(object label) where T : Object =>
        LoadContent<GameObject>(label).FirstOrDefault()?.GetComponent<T>() ?? throw ResourceException;

#endif

    private static Exception ResourceException => new InvalidOperationException("An internal resource failed to load");
}
