using UnityEngine;
using UnityEngine.UI;
#if BEAT_SABER_1_29_0
using System.Collections.Generic;
#endif

namespace Legato.Unity
{
    /// <summary>
    /// Applies padding when testing UI pointer hits.
    /// </summary>
    internal static class RaycastPadding
    {
#if BEAT_SABER_1_29_0
        private static readonly Dictionary<int, Vector4> Paddings = new();
#endif

        /// <summary>
        /// Sets the graphic's inset or expanded raycast bounds.
        /// </summary>
        /// <param name="graphic">The graphic to update.</param>
        /// <param name="padding">Padding for the four edges.</param>
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
        /// <summary>
        /// Tests a screen position against the rectangle's configured bounds.
        /// </summary>
        /// <param name="rect">The rectangle to test.</param>
        /// <param name="screenPoint">The pointer position in screen coordinates.</param>
        /// <param name="camera">The camera used to project the point.</param>
        /// <returns>Whether the position is inside those bounds.</returns>
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
                Paddings.Remove(this.RectId);
            }
        }
#endif
    }
}
