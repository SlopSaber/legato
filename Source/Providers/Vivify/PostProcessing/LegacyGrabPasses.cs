#if BEAT_SABER_1453
using UnityEngine;

namespace Vivify.PostProcessing;

internal static class LegacyGrabPasses
{
    internal static int Count => Legato.Rendering.AssetBundleGrabPasses.Count;

    internal static void Load(string? path) =>
        Legato.Rendering.AssetBundleGrabPasses.Load(path, message => Plugin.Log.Debug(message));

    internal static bool TryGetTextureName(Shader shader, int pass, out string textureName) =>
        Legato.Rendering.AssetBundleGrabPasses.TryGetTextureName(shader, pass, out textureName);
}
#endif
