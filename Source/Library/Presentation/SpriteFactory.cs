#nullable enable

#if BEAT_SABER_1_45_1
using System;
#endif
using BeatSaberMarkupLanguage;
using UnityEngine;

namespace Legato.Presentation {
    internal static class SpriteFactory {
#if BEAT_SABER_1_45_1
        private static readonly Func<Texture2D, byte[], bool, bool> LoadImage =
            (Func<Texture2D, byte[], bool, bool>)Delegate.CreateDelegate(
                typeof(Func<Texture2D, byte[], bool, bool>),
                typeof(ImageConversion).GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) })
                    ?? throw new MissingMethodException(typeof(ImageConversion).FullName, "LoadImage(Texture2D, byte[], bool)"));
#endif

        internal static Sprite Create(byte[] image) {
#if BEAT_SABER_1_45_1
            if (image == null) {
                throw new ArgumentNullException(nameof(image));
            }

            Texture2D texture = new Texture2D(2, 2);
            try {
                if (!LoadImage(texture, image, false)) {
                    throw new InvalidOperationException("Unity could not decode the sprite image.");
                }
                return Utilities.LoadSpriteFromTexture(texture);
            } catch {
                UnityEngine.Object.Destroy(texture);
                throw;
            }
#else
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(image);
            return Utilities.LoadSpriteFromTexture(texture);
#endif
        }

        internal static void Destroy(Sprite sprite) {
            if (sprite == null) {
                return;
            }

            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture != null) {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
