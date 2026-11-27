using System;
using System.Linq;
using UnityEngine;
using Zenject;
#if LEGATO_BEFORE_MULTIPLAYER
using UnityEngine.SceneManagement;
using SaberTrailRenderer = global::XWeaponTrailRenderer;
#elif BEAT_SABER_1_29_0
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
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        if (!TryGetModel(out var prefab)) return NativeSaberFallback.CreateModel(name);
        return SaberResources.CreateModel(prefab, name);
#else
        return SaberResources.CreateModel(Model, name);
#endif
    }

    internal Material GetDefaultTrailMaterial(bool shared)
    {
#if LEGATO_EARLY_STANDARD_MAPS
        if (!SaberResources.TryLoadNativeTrailRenderer(out _)) return NativeSaberFallback.TrailMaterial;
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        if (!TryGetModel(out _)) return NativeSaberFallback.TrailMaterial;
#endif
        return SaberTrailAccess.GetMaterial(Renderer, shared);
    }

    internal SaberTrailRenderer CreateTrailRenderer(DiContainer container)
    {
#if LEGATO_EARLY_STANDARD_MAPS
        if (!SaberResources.TryLoadNativeTrailRenderer(out _)) return NativeSaberFallback.CreateTrailRenderer();
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
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
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
    private static GameObject? nativeModel;
    private static bool observingAssets;
    private static int lastNativeLookupFrame = -1;
#endif
#if LEGATO_EARLY_STANDARD_MAPS
    private static XWeaponTrailRenderer? nativeTrailRenderer;

    internal static bool TryLoadNativeTrailRenderer(out XWeaponTrailRenderer renderer)
    {
        if (nativeTrailRenderer == null)
        {
            nativeTrailRenderer = Resources.FindObjectsOfTypeAll<SaberWeaponTrail>()
                .Select(trail => trail._trailRendererPrefab)
                .FirstOrDefault(prefab => prefab != null);
        }
        renderer = nativeTrailRenderer!;
        return renderer != null;
    }
#endif

    internal static void ObserveAssets()
    {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        if (observingAssets) return;
        observingAssets = true;
        SceneManager.sceneLoaded += CaptureNativeModel;
#endif
    }

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
    private static void CaptureNativeModel(Scene scene, LoadSceneMode mode)
    {
        lastNativeLookupFrame = -1;
        if (nativeModel == null) TryLoadModel(out _);
    }
#endif

    internal static SaberTrailRenderer LoadTrailRenderer() =>
#if LEGATO_EARLY_STANDARD_MAPS
        TryLoadNativeTrailRenderer(out var renderer) ? renderer : throw ResourceException;
#elif LEGATO_BEFORE_MULTIPLAYER
        LoadModel().GetComponent<BasicSaberModelController>()._saberWeaponTrail._trailRendererPrefab;
#elif BEAT_SABER_1_29_0
        LoadModel().GetComponent<SaberTrail>()._trailRendererPrefab;
#else
        LoadAsset<SaberTrailRenderer>("Assets/Prefabs/Effects/Sabers/SaberTrailRenderer.prefab");
#endif

    internal static GameObject LoadModel() =>
        TryLoadModel(out var model) ? model : throw ResourceException;

    internal static bool TryLoadModel(out GameObject model)
    {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        // Hold the gameplay asset for later menu previews without loading another scene.
        if (nativeModel == null && lastNativeLookupFrame != Time.frameCount)
        {
            lastNativeLookupFrame = Time.frameCount;
#if LEGATO_EARLY_STANDARD_MAPS
            var stock = Resources.FindObjectsOfTypeAll<Saber>()
                .FirstOrDefault(saber => saber.gameObject.scene.name == "GameCore" &&
                    saber.saberType == Saber.SaberType.SaberA);
            if (stock != null)
            {
                var preview = new GameObject("Stock Saber Preview") { layer = stock.gameObject.layer };
                preview.SetActive(false);
                CopyEarlyVisuals(stock.transform, preview.transform);
                Object.DontDestroyOnLoad(preview);
                nativeModel = preview;
            }
#elif LEGATO_BEFORE_MULTIPLAYER
            var candidate = Resources.FindObjectsOfTypeAll<BasicSaberModelController>()
                .FirstOrDefault(controller => controller.name == "BasicSaberModel" && !controller.gameObject.scene.IsValid());
            if (candidate != null && candidate._saberWeaponTrail != null && candidate._saberWeaponTrail._trailRendererPrefab != null)
#else
            var candidate = Resources.FindObjectsOfTypeAll<SaberModelController>()
                .FirstOrDefault(controller => controller.name == "BasicSaberModel" && !controller.gameObject.scene.IsValid());
            if (candidate != null && candidate.GetComponent<SaberTrail>()?._trailRendererPrefab != null)
#endif
#if !LEGATO_EARLY_STANDARD_MAPS
                nativeModel = candidate.gameObject;
#endif
        }
        model = nativeModel!;
#else
        model = LoadContent<GameObject>("Assets/Prefabs/Sabers/BasicSaberModel.prefab").FirstOrDefault()!;
#endif
        return model != null;
    }

    internal static SaberTrailRenderer InstantiateRenderer(DiContainer container, SaberTrailRenderer prefab) =>
#if LEGATO_BEFORE_MULTIPLAYER
        Object.Instantiate(prefab);
#elif LEGATO_NATIVE_ZENJECT_COMPONENT_BINDER
        container.InstantiatePrefabForComponentAt<SaberTrailRenderer>(prefab, Vector3.zero, Quaternion.identity, null);
#else
        container.InstantiatePrefabForComponent<SaberTrailRenderer>(prefab, Vector3.zero, Quaternion.identity, null);
#endif

    internal static GameObject CreateModel(GameObject prefab, string name)
    {
        var model = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        model.name = name;
#if LEGATO_EARLY_STANDARD_MAPS
        model.SetActive(true);
#else
        foreach (var glow in model.GetComponentsInChildren<SetSaberGlowColor>()) glow.enabled = false;
        foreach (var glow in model.GetComponentsInChildren<SetSaberFakeGlowColor>()) glow.enabled = false;
#if LEGATO_BEFORE_MULTIPLAYER
        model.GetComponent<BasicSaberModelController>()._saberWeaponTrail.enabled = false;
#else
        model.GetComponent<SaberTrail>().enabled = false;
#endif
#endif
        return model;
    }

#if LEGATO_EARLY_STANDARD_MAPS
    private static void CopyEarlyVisuals(Transform source, Transform destination)
    {
        var filter = source.GetComponent<MeshFilter>();
        var renderer = source.GetComponent<MeshRenderer>();
        if (filter != null && filter.sharedMesh != null && renderer != null)
        {
            destination.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var copy = destination.gameObject.AddComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            copy.shadowCastingMode = renderer.shadowCastingMode;
            copy.receiveShadows = renderer.receiveShadows;
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder;
            copy.enabled = renderer.enabled;
            var stockGlow = source.GetComponent<SetSaberGlowColor>();
            if (stockGlow != null)
            {
                var glow = destination.gameObject.AddComponent<SetSaberGlowColor>();
                glow.enabled = false;
                glow._meshRenderer = copy;
                glow._multiplierSaberColor = stockGlow._multiplierSaberColor;
            }
        }

        foreach (Transform child in source)
        {
            var part = new GameObject(child.name) { layer = child.gameObject.layer };
            part.transform.SetParent(destination, false);
            part.transform.localPosition = child.localPosition;
            part.transform.localRotation = child.localRotation;
            part.transform.localScale = child.localScale;
            CopyEarlyVisuals(child, part.transform);
            part.SetActive(child.gameObject.activeSelf);
        }
    }
#endif

    internal static void SetFallbackModelColor(GameObject model, Color color)
    {
#if LEGATO_EARLY_STANDARD_MAPS
        var fallback = model.GetComponent<NativeSaberFallbackColor>();
        if (fallback != null) fallback.SetColor(color);
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        if (model.TryGetComponent<NativeSaberFallbackColor>(out var fallback)) fallback.SetColor(color);
#endif
    }

#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
    private static T LoadAsset<T>(object label) where T : Object =>
        LoadContent<GameObject>(label).FirstOrDefault()?.GetComponent<T>() ?? throw ResourceException;

#endif

    private static Exception ResourceException => new InvalidOperationException("An internal resource failed to load");
}
