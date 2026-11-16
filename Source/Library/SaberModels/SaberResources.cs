using System;
using System.Linq;
using UnityEngine;
using static BGLib.UnityExtension.AddressablesExtensions;
using Object = UnityEngine.Object;

namespace Legato.Models;

internal static class SaberResources
{
    internal static SaberTrailRenderer LoadTrailRenderer() =>
        LoadAsset<SaberTrailRenderer>("Assets/Prefabs/Effects/Sabers/SaberTrailRenderer.prefab");

    internal static GameObject LoadModel() =>
        LoadContent<GameObject>("Assets/Prefabs/Sabers/BasicSaberModel.prefab").FirstOrDefault()
        ?? throw ResourceException;

    internal static GameObject CreateModel(GameObject prefab, string name)
    {
        var model = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        model.name = name;
        foreach (var glow in model.GetComponentsInChildren<SetSaberGlowColor>()) glow.enabled = false;
        foreach (var glow in model.GetComponentsInChildren<SetSaberFakeGlowColor>()) glow.enabled = false;
        model.GetComponent<SaberTrail>().enabled = false;
        return model;
    }

    private static T LoadAsset<T>(object label) where T : Object =>
        LoadContent<GameObject>(label).FirstOrDefault()?.GetComponent<T>() ?? throw ResourceException;

    private static Exception ResourceException => new InvalidOperationException("An internal resource failed to load");
}
