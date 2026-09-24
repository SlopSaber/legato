#nullable enable

#if BEAT_SABER_1_45_1
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
#endif
using BeatSaberMarkupLanguage;
using UnityEngine;

namespace Legato.Presentation {
    internal static class SpriteFactory {
        internal static Sprite Create(byte[] image) {
#if BEAT_SABER_1_45_1
            if (image == null) {
                throw new ArgumentNullException(nameof(image));
            }

            using var stream = new MemoryStream(image, writable: false);
            using var bitmap = new Bitmap(stream);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            int rowBytes = checked(bitmap.Width * 4);
            byte[] pixels = new byte[checked(rowBytes * bitmap.Height)];
            var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try {
                for (int row = 0; row < bitmap.Height; row++) {
                    Marshal.Copy(IntPtr.Add(data.Scan0, row * data.Stride), pixels, row * rowBytes, rowBytes);
                }
            } finally {
                bitmap.UnlockBits(data);
            }

            Texture2D texture = new Texture2D(bitmap.Width, bitmap.Height, TextureFormat.BGRA32, false);
            try {
                texture.LoadRawTextureData(pixels);
                texture.Apply(false, false);
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
