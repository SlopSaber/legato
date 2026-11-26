using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if !BEAT_SABER_1_29_0 && !LEGATO_NATIVE_POLYGLOT
using UnityEngine.AddressableAssets;
#else
using UnityEngine.SceneManagement;
#endif

namespace Legato.Assets {
    internal static class FontResources {
        internal static Task<TMP_FontAsset> LoadBoldAsync(CancellationToken cancellationToken = default) =>
#if BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
            WaitForNativeBoldAsync(cancellationToken);
#else
            Addressables.LoadAssetAsync<TMP_FontAsset>("Assets/Visuals/Fonts/Teko-Bold/Teko-Bold SDF.asset").Task;
#endif

#if BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
        internal static TMP_FontAsset RequireLoadedBold() => FindLoadedBold()
            ?? throw new InvalidOperationException("The native Teko bold font is not loaded.");

        private static TMP_FontAsset FindLoadedBold() => Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(font => font.name == "Teko-Bold SDF");

        private static async Task<TMP_FontAsset> WaitForNativeBoldAsync(CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            var loaded = FindLoadedBold();
            if (loaded != null) return loaded;

            var completion = new TaskCompletionSource<TMP_FontAsset>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnSceneLoaded(Scene _, LoadSceneMode __) {
                var font = FindLoadedBold();
                if (font != null) completion.TrySetResult(font);
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            try {
                using (cancellationToken.Register(() => {
                    SceneManager.sceneLoaded -= OnSceneLoaded;
                    completion.TrySetCanceled();
                })) {
                    OnSceneLoaded(default, default);
                    return await completion.Task;
                }
            }
            finally { SceneManager.sceneLoaded -= OnSceneLoaded; }
        }
#endif

        internal static Material NativeSdfMaterial => (Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(font => font.name == "Teko-Medium SDF")
            ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(font => font.name == "Teko-Bold SDF")
            ?? TMP_Settings.defaultFontAsset).material;
        internal static TextureCreationFlags CopyTextureFlags =>
#if LEGATO_BASIC_TEXTURE_CREATION_FLAGS || LEGATO_NATIVE_POLYGLOT
            TextureCreationFlags.None;
#else
            TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate;
#endif
        internal static void InvalidateFallbacks(TMP_FontAsset font) {
#if BEAT_SABER_1_29_0 || LEGATO_LEGACY_TEXTMESH_PRO || LEGATO_NATIVE_POLYGLOT
            font.ReadFontAssetDefinition();
#else
            font.ClearFallbackCharacterTable();
#endif
        }
    }
}
