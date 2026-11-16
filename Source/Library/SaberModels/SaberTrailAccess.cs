using UnityEngine;

namespace Legato.Models;

internal static class SaberTrailAccess
{
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
    internal static float GetDuration(SaberTrail trail) => trail._trailDuration;

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
}
