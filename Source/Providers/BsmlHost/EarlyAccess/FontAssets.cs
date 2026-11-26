#if LEGATO_EARLY_VR_UI
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeatSaberMarkupLanguage;

internal static class EarlyFontAssets
{
    private const int DefaultPointSize = 48;
    private static readonly string Characters =
        new(Enumerable.Range(32, 95).Concat(Enumerable.Range(160, 96)).Select(code => (char)code).ToArray());

    internal static TMP_FontAsset Create(Font font, string name)
    {
        if (font == null)
        {
            throw new ArgumentNullException(nameof(font));
        }

        const string shaderName = "TextMeshPro/Mobile/Bitmap";
        Shader shader = Shader.Find(shaderName) ??
            Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(candidate => candidate.name == shaderName);
        if (shader == null)
        {
            throw new InvalidOperationException("The game's TextMesh Pro bitmap shader is unavailable.");
        }

        int pointSize = font.fontSize > 0 ? font.fontSize : DefaultPointSize;
        int lookupSize = font.dynamic ? pointSize : 0;
        if (font.dynamic)
        {
            font.RequestCharactersInTexture(Characters, pointSize, FontStyle.Normal);
        }

        Texture source = font.material.mainTexture;
        if (source == null)
        {
            throw new InvalidOperationException($"Font '{font.name}' did not produce an atlas.");
        }

        List<TMP_Glyph> glyphs = new(Characters.Length);
        foreach (char character in Characters)
        {
            if (!font.GetCharacterInfo(character, out CharacterInfo glyph, lookupSize, FontStyle.Normal) ||
                !Mathf.Approximately(glyph.uvBottomLeft.x, glyph.uvTopLeft.x))
            {
                continue;
            }

            glyphs.Add(new TMP_Glyph
            {
                id = character,
                x = Mathf.Min(glyph.uvBottomLeft.x, glyph.uvTopRight.x) * source.width,
                y = (1f - Mathf.Max(glyph.uvBottomLeft.y, glyph.uvTopRight.y)) * source.height,
                width = glyph.glyphWidth,
                height = glyph.glyphHeight,
                xOffset = glyph.minX,
                yOffset = glyph.maxY,
                xAdvance = glyph.advance,
                scale = 1f,
            });
        }

        if (glyphs.Count == 0)
        {
            throw new InvalidOperationException($"Font '{font.name}' has no glyphs in the supported character range.");
        }

        string fontName = name ?? font.name;
        Texture2D atlas = CopyAtlas(source, fontName);
        TMP_FontAsset asset = ScriptableObject.CreateInstance<TMP_FontAsset>();
        try
        {
            asset.name = fontName;
            asset.fontAssetType = TMP_FontAsset.FontAssetTypes.Bitmap;
            asset.atlas = atlas;
            asset.material = new Material(shader)
            {
                name = $"{fontName} Atlas Material",
                mainTexture = atlas,
            };
            asset.AddFaceInfo(new FaceInfo
            {
                Name = fontName,
                PointSize = pointSize,
                Scale = 1f,
                CharacterCount = glyphs.Count,
                LineHeight = font.lineHeight,
                Ascender = font.ascent,
                CapHeight = font.ascent,
                Descender = font.ascent - font.lineHeight,
                SuperscriptOffset = font.ascent,
                SubscriptOffset = -pointSize * 0.1f,
                SubSize = 0.5f,
                Underline = -pointSize * 0.1f,
                UnderlineThickness = pointSize * 0.05f,
                AtlasWidth = atlas.width,
                AtlasHeight = atlas.height,
            });
            asset.AddGlyphInfo(glyphs.ToArray());
            asset.AddKerningInfo(new KerningTable());
            asset.ReadFontDefinition();
            return asset;
        }
        catch
        {
            Object.Destroy(asset.material);
            Object.Destroy(asset);
            Object.Destroy(atlas);
            throw;
        }
    }

    private static Texture2D CopyAtlas(Texture source, string name)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture buffer = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D atlas = new(source.width, source.height, TextureFormat.RGBA32, false, true)
        {
            name = $"{name} Atlas",
            wrapMode = TextureWrapMode.Clamp,
        };

        try
        {
            // Dynamic Unity font atlases can change size or move glyphs when more text is requested.
            Graphics.Blit(source, buffer);
            RenderTexture.active = buffer;
            atlas.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0, false);
            atlas.Apply(false, true);
            return atlas;
        }
        catch
        {
            Object.Destroy(atlas);
            throw;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(buffer);
        }
    }
}
#endif
