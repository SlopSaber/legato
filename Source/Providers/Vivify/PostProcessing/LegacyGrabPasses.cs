#if BEAT_SABER_1453
using System.Collections.Generic;
using UnityEngine;

namespace Vivify.PostProcessing;

internal static class LegacyGrabPasses
{
    internal static int Count => Legato.Rendering.AssetBundleGrabPasses.Count;

    internal static byte[]? Load(string? path, bool applyCompatibility = false) =>
        Legato.Rendering.AssetBundleGrabPasses.Load(
            path,
            applyCompatibility,
            !applyCompatibility || HasLegacyShadowCaster(),
            message => Plugin.Log.Debug(message));

    internal static bool TryGetTextureName(Shader shader, int pass, out string textureName) =>
        Legato.Rendering.AssetBundleGrabPasses.TryGetTextureName(shader, pass, out textureName);

    internal static bool HasGrabPass(Shader shader) => Legato.Rendering.AssetBundleGrabPasses.HasGrabPass(shader);

    internal static void CollectForwardGrabTextures(Shader shader, HashSet<string> textures) =>
        Legato.Rendering.AssetBundleGrabPasses.CollectForwardGrabTextures(shader, textures);

    private static bool HasLegacyShadowCaster()
    {
        Shader shader = Shader.Find("Legacy Shaders/VertexLit");
        if (shader == null || !shader.isSupported)
        {
            return false;
        }

        Material material = new(shader);
        try
        {
            return material.FindPass("SHADOWCASTER") >= 0;
        }
        finally
        {
            Object.Destroy(material);
        }
    }
}
#endif
