using UnityEngine;

namespace Legato.XR {
    internal static class ManualVrControl {
#if LEGATO_BEFORE_MULTIPLAYER
        internal static Transform PoseOrigin(PlayerController player) =>
            player._headTransform.parent != null ? player._headTransform.parent : player.transform;

        internal static void ApplyReplayHead(PlayerController player, Transform origin, Pose head) {
            player.OverrideHeadPos(origin.TransformPoint(head.position));
            player._headRot = origin.rotation * head.rotation;
        }

        internal static void Prepare(Saber saber) {
            saber._vrController.enabled = false;
            saber.disableCutting = true;
        }

        internal static void ApplySaberVisibility(PlayerController player, bool left, bool right) {
            var manager = player._saberManager;
            player.leftSaber.gameObject.SetActive(left &&
                (!manager._allowOnlyOneSaber || player.leftSaber.saberType == manager._oneSaberType));
            player.rightSaber.gameObject.SetActive(right &&
                (!manager._allowOnlyOneSaber || player.rightSaber.saberType == manager._oneSaberType));
        }
#else
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
#endif
    }
}
