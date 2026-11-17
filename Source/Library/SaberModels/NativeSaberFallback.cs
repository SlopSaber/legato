#if BEAT_SABER_1_29_0
using UnityEngine;

namespace Legato.Models;

internal static class NativeSaberFallback
{
    private static Material? modelMaterial;
    private static Material? trailMaterial;
    private static Mesh? partMesh;

    internal static Material TrailMaterial
    {
        get
        {
            if (trailMaterial != null) return trailMaterial;

            const int height = 16;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                name = "Fallback saber trail fade",
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[height];
            for (var y = 0; y < height; y++) pixels[y] = new Color(1f, 1f, 1f, 1f - y / (height - 1f));
            texture.SetPixels(pixels);
            texture.Apply(false, true);

            trailMaterial = CreateMaterial("Fallback saber trail");
            trailMaterial.mainTexture = texture;
            return trailMaterial;
        }
    }

    internal static GameObject CreateModel(string name)
    {
        var root = new GameObject(name);
        var blade = CreatePart(root.transform, "Blade", new Vector3(0.023f, 0.023f, 1f), 0.5f);
        var handle = CreatePart(root.transform, "Handle", new Vector3(0.048f, 0.048f, 0.16f), -0.08f);
        var color = root.AddComponent<NativeSaberFallbackColor>();
        color.Initialize(blade, handle);
        return root;
    }

    internal static SaberTrailRenderer CreateTrailRenderer()
    {
        var root = new GameObject("Fallback saber trail");
        root.AddComponent<MeshFilter>();
        root.AddComponent<MeshRenderer>().sharedMaterial = TrailMaterial;
        var renderer = root.AddComponent<SaberTrailRenderer>();
        SaberTrailAccess.InitializeRenderer(renderer);
        return renderer;
    }

    private static MeshRenderer CreatePart(Transform parent, string name, Vector3 scale, float center)
    {
        var part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = new Vector3(0f, 0f, center);
        part.transform.localScale = scale;
        if (partMesh == null) partMesh = CreatePartMesh();
        part.AddComponent<MeshFilter>().sharedMesh = partMesh;
        var renderer = part.AddComponent<MeshRenderer>();
        if (modelMaterial == null) modelMaterial = CreateMaterial("Fallback saber model");
        renderer.sharedMaterial = modelMaterial;
        return renderer;
    }

    private static Mesh CreatePartMesh()
    {
        const float h = 0.5f;
        var mesh = new Mesh
        {
            name = "Fallback saber part",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = [new(-h, -h, -h), new(h, -h, -h), new(h, h, -h), new(-h, h, -h),
                        new(-h, -h, h), new(h, -h, h), new(h, h, h), new(-h, h, h)],
            triangles = [0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 4, 7, 0, 7, 3,
                         1, 2, 6, 1, 6, 5, 0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2]
        };
        mesh.UploadMeshData(true);
        return mesh;
    }

    private static Material CreateMaterial(string name) => new(Shader.Find("Sprites/Default"))
    {
        name = name,
        hideFlags = HideFlags.HideAndDontSave
    };
}

internal sealed class NativeSaberFallbackColor : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private readonly MaterialPropertyBlock properties = new();
    private MeshRenderer blade = null!;

    internal void Initialize(MeshRenderer bladeRenderer, MeshRenderer handle)
    {
        blade = bladeRenderer;
        properties.SetColor(ColorProperty, new Color(0.18f, 0.18f, 0.18f));
        handle.SetPropertyBlock(properties);
    }

    internal void SetColor(Color color)
    {
        properties.SetColor(ColorProperty, color);
        blade.SetPropertyBlock(properties);
    }
}
#endif
