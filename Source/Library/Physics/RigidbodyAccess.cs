using UnityEngine;

namespace Legato.Physics {
    internal static class RigidbodyAccess {
        internal static Vector3 Velocity(Rigidbody body) =>
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            body.linearVelocity;
#else
            body.velocity;
#endif
        internal static void SetVelocity(Rigidbody body, Vector3 velocity) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }
        internal static void SetAngularDamping(Rigidbody body, float damping) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            body.angularDamping = damping;
#else
            body.angularDrag = damping;
#endif
        }
    }
}
