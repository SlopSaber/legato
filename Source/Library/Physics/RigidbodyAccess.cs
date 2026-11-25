using UnityEngine;

namespace Legato.Physics {
    internal static class RigidbodyAccess {
        internal static Vector3 Velocity(Rigidbody body) =>
#if LEGATO_UNITY_6_RIGIDBODY
            body.linearVelocity;
#else
            body.velocity;
#endif
        internal static void SetVelocity(Rigidbody body, Vector3 velocity) {
#if LEGATO_UNITY_6_RIGIDBODY
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
        internal static void SetAngularDamping(Rigidbody body, float damping) {
#if LEGATO_UNITY_6_RIGIDBODY
            body.angularDamping = damping;
#else
            body.angularDrag = damping;
#endif
        }
    }
}
