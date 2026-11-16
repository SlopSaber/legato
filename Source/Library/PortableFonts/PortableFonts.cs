using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace Legato.Assets {
    internal static class PortableFonts {
        private static readonly ConditionalWeakTable<TMP_FontAsset, OwnedResources> Owned = new();
        private sealed class OwnedResources {
            internal readonly List<Object> Items = new();
            internal void Release() { foreach (var item in Items) if (item != null) Object.Destroy(item); }
        }
        private sealed class FloatProperty { public string Name { get; set; } public float Value { get; set; } }
        private sealed class ColorProperty { public string Name { get; set; } public float R { get; set; } public float G { get; set; } public float B { get; set; } public float A { get; set; } }
        private sealed class Data {
            public string Name { get; set; }
            public string JsonData { get; set; }
            public string SourceBundle { get; set; }
            public string[] Textures { get; set; }
            public int PointSize { get; set; }
            public int Padding { get; set; }
            public int AtlasWidth { get; set; }
            public int AtlasHeight { get; set; }
            public int RenderMode { get; set; }
            public FloatProperty[] Floats { get; set; }
            public ColorProperty[] Colors { get; set; }
        }
        internal static TMP_FontAsset Load(Assembly assembly, string prefix, string name, Material nativeMaterial) {
            using var metadata = new StreamReader(Open(assembly, prefix + name + ".json"));
            var data = JsonConvert.DeserializeObject<Data>(metadata.ReadToEnd());
            var owned = new OwnedResources();
            TMP_FontAsset font = null;
            try {
                if (!string.IsNullOrEmpty(data.SourceBundle)) {
                    using var source = Open(assembly, prefix + data.SourceBundle);
                    var bundle = AssetBundle.LoadFromStream(source);
                    if (bundle == null) throw new InvalidDataException("The native font source bundle failed to load.");
                    Font sourceFont;
                    try { sourceFont = bundle.LoadAsset<Font>("assets/legato/source.font"); }
                    finally { bundle.Unload(false); }
                    if (sourceFont == null) throw new InvalidDataException("The native font source is missing.");
                    owned.Items.Add(sourceFont);
                    font = TMP_FontAsset.CreateFontAsset(sourceFont, data.PointSize, data.Padding,
                        (GlyphRenderMode)data.RenderMode, data.AtlasWidth, data.AtlasHeight, AtlasPopulationMode.Dynamic);
                    owned.Items.Add(font.material);
                    owned.Items.AddRange(font.atlasTextures);
                } else {
                    font = ScriptableObject.CreateInstance<TMP_FontAsset>();
                    JsonUtility.FromJsonOverwrite(data.JsonData, font);
                    var textures = new List<Texture2D>();
                    foreach (var filename in data.Textures) {
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        owned.Items.Add(texture);
                        using var stream = Open(assembly, prefix + filename);
                        using var bytes = new MemoryStream();
                        stream.CopyTo(bytes);
                        if (!texture.LoadImage(bytes.ToArray())) throw new InvalidDataException("The font atlas failed to decode: " + filename);
                        texture.name = filename;
                        texture.filterMode = FilterMode.Bilinear;
                        textures.Add(texture);
                    }
                    font.atlasTextures = textures.ToArray();
                    // Both serialized atlas fields must reference the same native texture.
                    IPA.Utilities.FieldAccessor<TMP_FontAsset, Texture2D>.GetAccessor("m_AtlasTexture")(ref font) = textures[0];
                    font.material = new Material(nativeMaterial);
                    owned.Items.Add(font.material);
                }
                font.name = data.Name;
                foreach (var property in data.Floats)
                    if (font.material.HasProperty(property.Name)) font.material.SetFloat(property.Name, property.Value);
                foreach (var property in data.Colors)
                    if (font.material.HasProperty(property.Name)) font.material.SetColor(property.Name, new Color(property.R, property.G, property.B, property.A));
                font.material.SetTexture("_MainTex", font.atlasTextures[0]);
                font.ReadFontAssetDefinition();
                Owned.Add(font, owned);
                return font;
            } catch {
                owned.Release();
                if (font != null) Object.Destroy(font);
                throw;
            }
        }
        internal static void Release(TMP_FontAsset font) {
            if (font == null) return;
            if (Owned.TryGetValue(font, out var owned)) { Owned.Remove(font); owned.Release(); }
            Object.Destroy(font);
        }
        private static Stream Open(Assembly assembly, string resource) => assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException("The portable font resource is missing.", resource);
    }
}
