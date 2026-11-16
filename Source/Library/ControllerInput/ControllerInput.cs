using IPA.Utilities;
using UnityEngine;
using VRUIControls;

namespace Legato.Unity
{
    internal static class ControllerInput
    {
#if BEAT_SABER_1_29_0
        private static readonly FieldAccessor<VRController, IVRPlatformHelper>.Accessor Platform =
            FieldAccessor<VRController, IVRPlatformHelper>.GetAccessor("_vrPlatformHelper");
        private static readonly FieldAccessor<VRPointer, Transform>.Accessor Cursor =
            FieldAccessor<VRPointer, Transform>.GetAccessor("_cursorTransform");
#endif

        internal static bool PoseIsValid(VRController controller)
        {
#if BEAT_SABER_1_29_0
            return Platform(ref controller).GetNodePose(controller.node, controller.nodeIdx, out _, out _);
#else
            return controller.poseValid;
#endif
        }

        internal static Transform ViewAnchor(VRController controller)
        {
#if BEAT_SABER_1_29_0
            return controller.transform;
#else
            return controller.viewAnchorTransform;
#endif
        }

        internal static Transform PointerCursor(VRPointer pointer)
        {
#if BEAT_SABER_1_29_0
            return Cursor(ref pointer);
#else
            return pointer.cursorTransform;
#endif
        }
    }
}
