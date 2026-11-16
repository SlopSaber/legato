using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if !BEAT_SABER_1_29_0
using UnityEngine.AddressableAssets;
#endif

namespace Legato.Assets {
    internal static class FontResources {
        internal static Task<TMP_FontAsset> LoadBoldAsync() =>
#if BEAT_SABER_1_29_0
            Task.FromResult(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(font => font.name == "Teko-Bold SDF")
                ?? throw new InvalidOperationException("The native Teko bold font is not loaded."));
#else
            Addressables.LoadAssetAsync<TMP_FontAsset>("Assets/Visuals/Fonts/Teko-Bold/Teko-Bold SDF.asset").Task;
#endif
        internal static Material NativeSdfMaterial => (Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(font => font.name == "Teko-Medium SDF") ?? TMP_Settings.defaultFontAsset).material;
        internal static TextureCreationFlags CopyTextureFlags =>
#if BEAT_SABER_1_29_0
            TextureCreationFlags.None;
#else
            TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate;
#endif
        internal static void InvalidateFallbacks(TMP_FontAsset font) {
#if BEAT_SABER_1_29_0
            font.ReadFontAssetDefinition();
#else
            font.ClearFallbackCharacterTable();
#endif
        }
    }
}
