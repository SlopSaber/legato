#if BEAT_SABER_1453
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Legato.Rendering;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vivify.Managers;

// TextMeshPro dereferences both the text's shared material and its font's material while
// building geometry. Bundles made with older TextMeshPro versions can lose either one,
// which throws on every canvas rebuild, so missing materials are recreated from the atlas.
internal sealed class LegacyTextCompatibility
{
    private const string DistanceFieldShader = "TextMeshPro/Distance Field";
    private const string MobileDistanceFieldShader = "TextMeshPro/Mobile/Distance Field";
    private const string NativeDistanceFieldFont = "Assets/Visuals/Fonts/Test Fallback Fonts/NotoSansKR-Medium SDF.asset";
    private const string NativeMobileDistanceFieldFont = "Assets/Visuals/Fonts/Teko-Bold/Teko-Bold SDF.asset";
    private static readonly int _colorMaskProperty = Shader.PropertyToID("_ColorMask");

    [UsedImplicitly]
    private LegacyTextCompatibility(AssetBundleManager assetBundleManager, SiraLog log)
    {
        int repaired = 0;
        bool legacyBundle = IsPreUnity6Bundle(assetBundleManager.BundlePath);
        List<TMP_Text> texts = [];
        List<TMP_SubMesh> subMeshes = [];
        List<TMP_SubMeshUI> uiSubMeshes = [];
        HashSet<TMP_FontAsset> visitedFonts = [];
        HashSet<Material> visitedMaterials = [];
        Dictionary<string, Shader?> nativeShaders = new(StringComparer.Ordinal);
        int updatedShaders = 0;
        foreach (Object asset in assetBundleManager.Assets)
        {
            switch (asset)
            {
                case TMP_FontAsset font:
                    if (EnsureFontMaterial(font))
                    {
                        repaired++;
                    }

                    if (legacyBundle)
                    {
                        UpdateFontShaders(font);
                    }

                    break;

                case GameObject prefab:
                    prefab.GetComponentsInChildren(true, texts);
                    foreach (TMP_Text text in texts)
                    {
                        if (EnsureTextMaterial(text))
                        {
                            repaired++;
                        }

                        if (legacyBundle)
                        {
                            UpdateFontShaders(text.font);
                            UpdateShader(text.fontSharedMaterial);
                        }
                    }

                    if (legacyBundle && texts.Count > 0)
                    {
                        prefab.GetComponentsInChildren(true, subMeshes);
                        foreach (TMP_SubMesh subMesh in subMeshes)
                        {
                            UpdateShader(subMesh.sharedMaterial);
                        }

                        prefab.GetComponentsInChildren(true, uiSubMeshes);
                        foreach (TMP_SubMeshUI subMesh in uiSubMeshes)
                        {
                            UpdateShader(subMesh.sharedMaterial);
                        }
                    }

                    break;
            }
        }

        if (repaired > 0)
        {
            log.Info($"Restored {repaired} missing TextMeshPro material(s) in bundled assets.");
        }

        if (updatedShaders > 0)
        {
            log.Info($"Updated {updatedShaders} bundled TextMeshPro material(s) for the current glyph UV layout.");
        }

        void UpdateFontShaders(TMP_FontAsset? font)
        {
            if (font == null || !visitedFonts.Add(font))
            {
                return;
            }

            UpdateShader(font.material);
            if (font.fallbackFontAssetTable != null)
            {
                foreach (TMP_FontAsset fallback in font.fallbackFontAssetTable)
                {
                    UpdateFontShaders(fallback);
                }
            }
        }

        void UpdateShader(Material? material)
        {
            if (material == null || material.shader == null || !visitedMaterials.Add(material))
            {
                return;
            }

            Shader original = material.shader;
            string name = original.name;
            if (name != DistanceFieldShader && name != MobileDistanceFieldShader)
            {
                return;
            }

            if (!nativeShaders.TryGetValue(name, out Shader? nativeShader))
            {
                nativeShader = FindNativeShader(name);
                nativeShaders.Add(name, nativeShader);
                if (nativeShader == null)
                {
                    log.Warn($"Could not find the game shader [{name}] for bundled TextMeshPro materials.");
                }
            }

            if (nativeShader == null || nativeShader == original)
            {
                return;
            }

            // Keep the authored atlas, settings and material identity for TMP and map events.
            int renderQueue = material.rawRenderQueue;
            string[] keywords = material.shaderKeywords;
            int? colorMask = AssetBundleGrabPasses.GetFixedTextColorMask(original);
            material.shader = nativeShader;
            if (colorMask is int fixedColorMask && material.HasProperty(_colorMaskProperty))
            {
                material.SetInt(_colorMaskProperty, fixedColorMask);
            }

            material.shaderKeywords = keywords;
            material.renderQueue = renderQueue;
            updatedShaders++;
        }
    }

    // Older font assets are upgraded on first use, so instances are checked again after
    // forcing that upgrade. Text that still has no usable material is disabled instead of
    // throwing on every rebuild.
    internal static int RepairInstance(GameObject instance, bool awake)
    {
        int disabled = 0;
        foreach (TMP_Text text in instance.GetComponentsInChildren<TMP_Text>(true))
        {
            TMP_FontAsset? font = text.font;
            if (font != null)
            {
                font.ReadFontAssetDefinition();
            }

            EnsureTextMaterial(text);
            bool unusable = awake && text.gameObject.activeInHierarchy
                ? text.font == null || text.font.material == null || text.fontSharedMaterial == null
                : text.font != null && text.font.material == null;
            if (unusable && text.enabled)
            {
                text.enabled = false;
                disabled++;
            }
        }

        return disabled;
    }

    private static bool IsPreUnity6Bundle(string? path)
    {
        if (path == null || !File.Exists(path))
        {
            return false;
        }

        try
        {
            using BinaryReader reader = new(File.OpenRead(path));

            // UnityFS signature and binary format version occupy the first 12 bytes.
            byte[] header = reader.ReadBytes(128);
            if (header.Length <= 12 || Encoding.ASCII.GetString(header, 0, 8) != "UnityFS\0")
            {
                return false;
            }

            string[] fields = Encoding.ASCII.GetString(header, 12, header.Length - 12).Split('\0');
            int separator = fields.Length > 1 ? fields[1].IndexOf('.') : -1;
            return separator > 0 && int.TryParse(fields[1].Substring(0, separator), out int major) && major < 6000;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static Shader? FindNativeShader(string name)
    {
        string fontPath = name == DistanceFieldShader ? NativeDistanceFieldFont : NativeMobileDistanceFieldFont;
        foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (!bundle.Contains(NativeMobileDistanceFieldFont) || !bundle.Contains(fontPath))
            {
                continue;
            }

            // The map and the game can both have a shader named this way.
            Shader? shader = bundle.LoadAsset<TMP_FontAsset>(fontPath)?.material?.shader;
            if (shader != null && shader.name == name && shader.isSupported)
            {
                return shader;
            }
        }

        return null;
    }

    private static bool EnsureTextMaterial(TMP_Text text)
    {
        TMP_FontAsset? font = text.font;
        if (font == null)
        {
            return false;
        }

        bool repaired = EnsureFontMaterial(font);
        if (text.fontSharedMaterial == null && font.material != null)
        {
            text.fontSharedMaterial = font.material;
            repaired = true;
        }

        return repaired;
    }

    private static bool EnsureFontMaterial(TMP_FontAsset font)
    {
        if (font.material != null)
        {
            return false;
        }

        Shader? shader = Shader.Find(DistanceFieldShader) ?? FindLoadedTextShader();
        Texture2D? atlas = font.atlasTexture;
        if (shader == null || atlas == null)
        {
            return false;
        }

        Material material = new(shader) { name = $"{font.name} Material" };
        material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
        material.SetFloat(ShaderUtilities.ID_TextureWidth, atlas.width);
        material.SetFloat(ShaderUtilities.ID_TextureHeight, atlas.height);
        material.SetFloat(ShaderUtilities.ID_GradientScale, font.atlasPadding + 1);
        material.SetFloat(ShaderUtilities.ID_WeightNormal, font.normalStyle);
        material.SetFloat(ShaderUtilities.ID_WeightBold, font.boldStyle);
        font.material = material;
        return true;
    }

    private static Shader? FindLoadedTextShader()
    {
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (font != null && font.material != null && font.material.shader != null && font.material.shader.isSupported)
            {
                return font.material.shader;
            }
        }

        return null;
    }
}
#endif
