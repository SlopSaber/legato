#if LEGATO_BUILT_IN_RENDERING && !COMPILE_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Reactive.BeatSaber;
using UnityEngine;
#if !BEAT_SABER_1_29_0
using System.Linq;
#endif

namespace Legato.Reactive
{
    internal static class LegacyUiResources
    {
        private const string ResourcePrefix = "Legato.ReactiveUiLegacy.";
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        internal static BeatSaberResources Create()
        {
            var result = ScriptableObject.CreateInstance<BeatSaberResources>();
            result.sprites = ScriptableObject.CreateInstance<SpriteCollection>();
            result.materials = ScriptableObject.CreateInstance<MaterialCollection>();
            result.sprites.rectangle = Sprite.Create(LoadTexture("texture-0.png", 1, 1), new Rect(40.959999084472656f, 40.959999084472656f, 46.07999801635742f, 46.07999801635742f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            result.sprites.rectangle.name = "Rectangle";
            result.sprites.background = Sprite.Create(LoadTexture("texture-0.png", 1, 1), new Rect(0.0f, 0.0f, 128.0f, 128.0f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(40.959999084472656f, 40.959999084472656f, 40.959999084472656f, 40.959999084472656f));
            result.sprites.background.name = "Background";
            result.sprites.backgroundLeft = Sprite.Create(LoadTexture("texture-0.png", 1, 1), new Rect(0.0f, 0.0f, 40.959999084472656f, 128.0f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(40.959999084472656f, 40.959999084472656f, 0.0f, 40.959999084472656f));
            result.sprites.backgroundLeft.name = "Left";
            result.sprites.backgroundRight = Sprite.Create(LoadTexture("texture-0.png", 1, 1), new Rect(87.04000091552734f, 0.0f, 40.959999084472656f, 128.0f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(0.0f, 40.959999084472656f, 40.959999084472656f, 40.959999084472656f));
            result.sprites.backgroundRight.name = "Right";
            result.sprites.backgroundBottom = Sprite.Create(LoadTexture("texture-0.png", 1, 1), new Rect(0.0f, 0.0f, 128.0f, 40.959999084472656f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(40.959999084472656f, 40.959999084472656f, 40.959999084472656f, 0.0f));
            result.sprites.backgroundBottom.name = "Bottom";
            result.sprites.backgroundTop = Sprite.Create(LoadTexture("texture-0.png", 1, 1), new Rect(0.0f, 87.04000091552734f, 128.0f, 40.959999084472656f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(40.959999084472656f, 0.0f, 40.959999084472656f, 40.959999084472656f));
            result.sprites.backgroundTop.name = "Top";
            result.sprites.backgroundUnderline = Sprite.Create(LoadTexture("texture-1.png", 1, 1), new Rect(0.0f, 0.0f, 128.0f, 128.0f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(40.959999084472656f, 40.959999084472656f, 40.959999084472656f, 40.959999084472656f));
            result.sprites.backgroundUnderline.name = "Underline";
            result.sprites.spinnerIcon = Sprite.Create(LoadTexture("texture-2.png", 1, 1), new Rect(0.0f, 0.0f, 128.0f, 128.0f), new Vector2(0.5f, 0.5f), 50.0f, 0, SpriteMeshType.FullRect, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            result.sprites.spinnerIcon.name = "Spinner";
            result.sprites.crossIcon = Sprite.Create(LoadTexture("texture-3.png", 1, 1), new Rect(0.0f, 0.0f, 128.0f, 128.0f), new Vector2(0.5f, 0.5f), 50.0f, 0, SpriteMeshType.FullRect, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            result.sprites.crossIcon.name = "Cross";
            result.sprites.frame = Sprite.Create(LoadTexture("texture-4.png", 1, 1), new Rect(0.0f, 0.0f, 128.0f, 128.0f), new Vector2(0.5f, 0.5f), 32.0f, 0, SpriteMeshType.FullRect, new Vector4(40.959999084472656f, 40.959999084472656f, 40.959999084472656f, 40.959999084472656f));
            result.sprites.frame.name = "Frame";
            result.sprites.transparentPixel = Sprite.Create(LoadTexture("texture-5.png", 1, 1), new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            result.sprites.transparentPixel.name = "TransparentPixel";
#if !BEAT_SABER_1_29_0
            result.materials.uiAdditiveGlowMaterial = Legato.Unity.ObjectDiscovery.FindLoaded<Material>()
                .First(material => material.name == "UINoGlowAdditive");
#else
            var bundle = AssetBundle.LoadFromMemory(ReadResource("ui-2019.bundle"));
            if (bundle == null)
            {
                throw new InvalidOperationException("The legacy UI material bundle could not be loaded.");
            }

            try
            {
                result.materials.uiAdditiveGlowMaterial = bundle.LoadAsset<Material>("assets/legato/uiadditiveglow.mat");
                if (result.materials.uiAdditiveGlowMaterial == null)
                {
                    throw new InvalidOperationException("The legacy UI additive material was not found.");
                }
            }
            finally
            {
                bundle.Unload(false);
            }
#endif

            return result;
        }

        private static Texture2D LoadTexture(string filename, int filter, int wrap)
        {
            if (Textures.TryGetValue(filename, out var existing))
            {
                return existing;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(ReadResource(filename)))
            {
                UnityEngine.Object.Destroy(texture);
                throw new InvalidOperationException("The UI texture could not be decoded: " + filename);
            }

            texture.name = filename;
            texture.filterMode = (FilterMode)filter;
            texture.wrapMode = (TextureWrapMode)wrap;
            Textures.Add(filename, texture);
            return texture;
        }

        private static byte[] ReadResource(string filename)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + filename))
            using (var bytes = new MemoryStream())
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("The UI resource was not found: " + filename);
                }

                stream.CopyTo(bytes);
                return bytes.ToArray();
            }
        }
    }
}
#endif
