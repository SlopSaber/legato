using UnityEngine;

namespace Legato.XR {
    internal static class ManualVrControl {
        internal static Behaviour FocusCapture(PlayerVRControllersManager controllers) =>
#if LEGATO_CONTROLLER_FOCUS_CAPTURE
            controllers.GetComponent<DeactivateVRControllersOnFocusCapture>();
#else
            null;
#endif
        internal static bool Autoplay(VRController controller) =>
#if LEGATO_CONTROLLER_AUTOPLAY
            controller.autoPlayActive;
#else
            false;
#endif
        internal static void SetAutoplay(VRController controller, bool active) {
#if LEGATO_CONTROLLER_AUTOPLAY
            controller.autoPlayActive = active;
#endif
        }
        internal static void Prepare(PlayerVRControllersManager controllers) {
#if LEGATO_CONTROLLER_AUTOPLAY
            controllers.SetupAutoplayForAllControllers();
#else
            controllers.leftHandVRController.enabled = false;
            controllers.rightHandVRController.enabled = false;
#endif
        }
    }
}
