#if LEGATO_BEFORE_MULTIPLAYER
using System.Linq;
using UnityEngine;
using Xft;

namespace Legato.Rendering {
    internal static class LegacySaberTrails {
        internal static XWeaponTrail? Create(Transform root, Transform tip, float duration, Color color, bool enabled) {
            var prefab = UnityEngine.Resources.FindObjectsOfTypeAll<XWeaponTrail>()
                .Select(trail => trail._trailRendererPrefab)
                .FirstOrDefault(renderer => renderer != null);
            if (prefab == null) return null;

            var trailObject = new GameObject("BattleRoyaleTrail");
            trailObject.transform.SetParent(root, false);
            var trail = trailObject.AddComponent<XWeaponTrail>();
            trail._trailRendererPrefab = prefab;
            trail._pointStart = tip;
            trail._pointEnd = root;
            var framesPerSecond = Time.smoothDeltaTime > 0f ? 1f / Time.smoothDeltaTime : 60f;
            trail._maxFrame = Mathf.Clamp(Mathf.RoundToInt(duration * framesPerSecond), 3, 180);
            trail._granularity = 45;
            trail._whiteSteps = 0;
            trail.color = color;
            trail.enabled = enabled;
            return trail;
        }

        internal static void Droop(XWeaponTrail trail, float amount) {
            var segment = trail._vertexSegment;
            var pool = segment.Pool;
            for (var i = segment.VertStart; i < segment.VertStart + segment.VertCount; i++) {
                var age = pool.uvs[i].y;
                pool.vertices[i].y -= amount * age * age;
            }
        }
    }
}
#endif
