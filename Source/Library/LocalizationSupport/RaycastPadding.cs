using UnityEngine;
using UnityEngine.UI;
#if BEAT_SABER_1_29_0
using System.Collections.Generic;
#endif

namespace Legato.Unity
{
    internal static class RaycastPadding
    {
#if BEAT_SABER_1_29_0
        private static readonly Dictionary<int, Vector4> Paddings = new();
#endif

        internal static void Set(Graphic graphic, Vector4 padding)
        {
#if BEAT_SABER_1_29_0
            int id = graphic.rectTransform.GetInstanceID();
            Paddings[id] = padding;
            PaddingLifetime lifetime = graphic.GetComponent<PaddingLifetime>();
            if (lifetime == null)
            {
                lifetime = graphic.gameObject.AddComponent<PaddingLifetime>();
            }

            lifetime.RectId = id;
#else
            graphic.raycastPadding = padding;
#endif
        }

#if BEAT_SABER_1_29_0
        internal static bool Contains(RectTransform rect, Vector2 screenPoint, Camera camera)
        {
            return Paddings.TryGetValue(rect.GetInstanceID(), out Vector4 padding)
                ? RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera, padding)
                : RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera);
        }

        private sealed class PaddingLifetime : MonoBehaviour
        {
            internal int RectId { get; set; }

            private void OnDestroy()
            {
                Paddings.Remove(RectId);
            }
        }
#endif
    }
}
