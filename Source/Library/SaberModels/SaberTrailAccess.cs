using UnityEngine;
#if LEGATO_BEFORE_MULTIPLAYER
using Xft;
#endif

namespace Legato.Models;

internal static class SaberTrailAccess
{
#if LEGATO_BEFORE_MULTIPLAYER
    private const float LegacyFramesPerSecond = 75f;

    internal static void Initialize(XWeaponTrail trail, float duration, int granularity, Color color,
        XWeaponTrailRenderer renderer, Transform top, Transform bottom)
    {
        trail._trailRendererPrefab = renderer;
        renderer.gameObject.SetActive(false);
        trail._pointStart = top;
        trail._pointEnd = bottom;
        trail._granularity = granularity;
        trail.color = color;
        SetDuration(trail, duration);
        if (duration <= 0f) trail.enabled = false;
    }

    internal static void InitializeRenderer(XWeaponTrailRenderer renderer)
    {
        renderer._meshRenderer = renderer.GetComponent<MeshRenderer>();
        renderer._meshFilter = renderer.GetComponent<MeshFilter>();
    }

    internal static Material GetMaterial(XWeaponTrailRenderer renderer, bool shared) =>
        shared ? renderer._meshRenderer.sharedMaterial : renderer._meshRenderer.material;

    internal static void SetMaterial(XWeaponTrailRenderer renderer, Material material)
    {
        renderer._meshRenderer.sharedMaterial = material;
    }

    internal static void SetColor(XWeaponTrail trail, Color color, int property)
    {
        trail.color = color;
        if (trail._trailRenderer == null) return;
        var renderer = trail._trailRenderer._meshRenderer;
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetColor(property, color);
        renderer.SetPropertyBlock(properties);
    }

    internal static bool HasRenderer(XWeaponTrail trail) => trail._trailRendererPrefab != null || trail._trailRenderer != null;

    internal static void Reset(XWeaponTrail trail, float whiteSectionDuration)
    {
        trail._whiteSteps = whiteSectionDuration > 0f ? 4 : 0;
        trail._frameNum = 0;
    }

    internal static void SetDuration(XWeaponTrail trail, float duration)
    {
        trail._maxFrame = Mathf.Max(2, Mathf.RoundToInt(duration * LegacyFramesPerSecond));
    }

    internal static void MapAgeCoordinate(XWeaponTrail trail, bool flip)
    {
        if (!flip) return;

        var segment = trail._vertexSegment;
        var uvs = segment.Pool.uvs;
        int end = segment.VertStart + trail._granularity * 3;
        for (int i = segment.VertStart; i < end; i++)
            uvs[i].y = 1f - uvs[i].y;
    }
#else
    internal static void Initialize(SaberTrail trail, float duration, int samplingFrequency,
        int granularity, Color color, SaberTrailRenderer renderer)
    {
        trail._trailDuration = duration;
        trail._samplingFrequency = samplingFrequency;
        trail._granularity = granularity;
        trail._color = color;
        trail._trailRenderer = renderer;
    }

    internal static void InitializeRenderer(SaberTrailRenderer renderer)
    {
        renderer._meshRenderer = renderer.GetComponent<MeshRenderer>();
        renderer._meshFilter = renderer.GetComponent<MeshFilter>();
    }

    internal static Material GetMaterial(SaberTrailRenderer renderer, bool shared) =>
        shared ? renderer._meshRenderer.sharedMaterial : renderer._meshRenderer.material;

    internal static void SetMaterial(SaberTrailRenderer renderer, Material material, Color color)
    {
        renderer._meshRenderer.material = material;
        renderer._meshRenderer.material.color = color;
    }

    internal static void SetMovementData(SaberTrail trail, SaberMovementData data) => trail._movementData = data;

    internal static void SetColor(SaberTrail trail, Color color, int property)
    {
        trail._color = color;
        foreach (var material in trail._trailRenderer._meshRenderer.materials)
            material.SetColor(property, trail._color);
    }

    internal static bool HasRenderer(SaberTrail trail) => trail._trailRenderer != null;

    internal static void Reset(SaberTrail trail, float whiteSectionDuration)
    {
        trail._whiteSectionMaxDuration = whiteSectionDuration;
        trail._framesPassed = 0;
        trail._framesToScaleCheck = 0;
        trail._inited = false;
    }

    internal static void SetDuration(SaberTrail trail, float duration) => trail._trailDuration = duration;
    internal static void MapAgeCoordinate(SaberTrailRenderer renderer, bool flip)
    {
        // Extra samples can extend beyond the duration. Clamp before flipping to avoid texture wrap.
        var uvs = renderer._uvs;
        for (int i = 0; i < uvs.Length; i++)
        {
            var age = Mathf.Clamp01(uvs[i].y);
            uvs[i].y = flip ? 1f - age : age;
        }
    }
#endif
}
