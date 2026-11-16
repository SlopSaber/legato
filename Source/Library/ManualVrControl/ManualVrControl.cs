using UnityEngine;

namespace Legato.XR {
    internal static class ManualVrControl {
        internal static Behaviour FocusCapture(PlayerVRControllersManager controllers) =>
#if BEAT_SABER_1_29_0
            null;
#else
            controllers.GetComponent<DeactivateVRControllersOnFocusCapture>();
#endif
        internal static bool Autoplay(VRController controller) =>
#if BEAT_SABER_1_29_0
            false;
#else
            controller.autoPlayActive;
#endif
        internal static void SetAutoplay(VRController controller, bool active) {
#if !BEAT_SABER_1_29_0
            controller.autoPlayActive = active;
#endif
        }
        internal static void Prepare(PlayerVRControllersManager controllers) {
#if BEAT_SABER_1_29_0
            controllers.leftHandVRController.enabled = false;
            controllers.rightHandVRController.enabled = false;
#else
            controllers.SetupAutoplayForAllControllers();
#endif
        }
    }
}
