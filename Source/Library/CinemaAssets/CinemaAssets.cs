#nullable enable
using System;
using System.Linq;
using UnityEngine;

namespace Legato.Rendering {
    internal static class CinemaAssets {
        internal static Shader? NativeBody(string assetName, string? path) {
#if BEAT_SABER_1_29_0
            if (path == null && assetName == "ScreenBody")
                return Resources.FindObjectsOfTypeAll<Shader>().LastOrDefault(shader => shader.name == "Custom/OpaqueNeonLight")
                    ?? Shader.Find("Unlit/Color")
                    ?? throw new InvalidOperationException("The native opaque screen body shader is unavailable.");
#endif
            return null;
        }
    }
}
