using System;
using System.Linq;
using UnityEngine;
using Zenject;
#if !BEAT_SABER_1_29_0
using static BGLib.UnityExtension.AddressablesExtensions;
#endif
using Object = UnityEngine.Object;

namespace Legato.Models;

internal sealed class SaberResourceCache
{
    private GameObject? model;
    private SaberTrailRenderer? renderer;

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
    internal static SaberTrailRenderer LoadTrailRenderer() =>
#if BEAT_SABER_1_29_0
        LoadModel().GetComponent<SaberTrail>()._trailRendererPrefab;
#else
        LoadAsset<SaberTrailRenderer>("Assets/Prefabs/Effects/Sabers/SaberTrailRenderer.prefab");
#endif

    internal static GameObject LoadModel() =>
#if BEAT_SABER_1_29_0
        Resources.FindObjectsOfTypeAll<SaberModelController>()
            .Where(model => model.name == "BasicSaberModel" || model.name == "BasicSaberModel(Clone)")
            .OrderBy(model => model.gameObject.scene.IsValid())
            .Select(model => model.gameObject).FirstOrDefault() ?? throw ResourceException;
#else
        LoadContent<GameObject>("Assets/Prefabs/Sabers/BasicSaberModel.prefab").FirstOrDefault()
        ?? throw ResourceException;
#endif

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
