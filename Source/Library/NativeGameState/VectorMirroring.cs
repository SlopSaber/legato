using UnityEngine;

namespace Legato.Geometry {
    internal static class VectorMirroring {
        internal static Vector3 MirrorX(Vector3 value) => new(-value.x, value.y, value.z);
    }
}
