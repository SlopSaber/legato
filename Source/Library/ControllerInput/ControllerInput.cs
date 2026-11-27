using IPA.Utilities;
using UnityEngine;
using VRUIControls;
#if LEGATO_EARLY_STANDARD_MAPS
using UnityEngine.XR;
#elif LEGATO_QUERY_CONTROLLER_POSE
#if LEGATO_BEFORE_MULTIPLAYER
using NativeInputPlatform = global::VRPlatformHelper;
#else
using NativeInputPlatform = global::IVRPlatformHelper;
#endif
#endif

namespace Legato.Unity
{
    internal static class ControllerInput
    {
        internal static VRController SelectedController(VRPointer pointer) =>
#if LEGATO_EARLY_STANDARD_MAPS || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
            pointer.vrController;
#else
            pointer.lastSelectedVrController;
#endif
        internal static float VerticalAxis(VRController controller) =>
#if LEGATO_EARLY_STANDARD_MAPS || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
            controller.verticalAxisValue;
#else
            controller.thumbstick.y;
#endif
#if LEGATO_QUERY_CONTROLLER_POSE && !LEGATO_EARLY_STANDARD_MAPS
        private static readonly FieldAccessor<VRController, NativeInputPlatform>.Accessor Platform =
            FieldAccessor<VRController, NativeInputPlatform>.GetAccessor("_vrPlatformHelper");
#endif
#if LEGATO_PRIVATE_POINTER_CURSOR || LEGATO_EARLY_STANDARD_MAPS
        private static readonly FieldAccessor<VRPointer, Transform>.Accessor Cursor =
            FieldAccessor<VRPointer, Transform>.GetAccessor("_cursorTransform");
#endif

        internal static bool PoseIsValid(VRController controller)
        {
#if LEGATO_EARLY_STANDARD_MAPS
            return controller.active && InputTracking.GetLocalPosition(controller.node) != Vector3.zero;
#elif LEGATO_QUERY_CONTROLLER_POSE
            return Platform(ref controller)?.GetNodePose(controller.node, controller.nodeIdx, out _, out _) == true;
#else
            return controller.poseValid;
#endif
        }

        internal static Transform ViewAnchor(VRController controller)
        {
#if LEGATO_EARLY_STANDARD_MAPS || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
            return controller.transform;
#else
            return controller.viewAnchorTransform;
#endif
        }

        internal static Transform PointerCursor(VRPointer pointer)
        {
#if LEGATO_PRIVATE_POINTER_CURSOR || LEGATO_EARLY_STANDARD_MAPS
            return Cursor(ref pointer);
#else
            return pointer.cursorTransform;
#endif
        }
    }
}
