#if !COMPILE_EDITOR
#if LEGATO_BEFORE_MULTIPLAYER
global using NativeJoystickInput = global::VRControllersInputManager;
using UnityEngine.XR;
#else
global using NativeJoystickInput = global::IVRPlatformHelper;
#endif
using System;
using UnityEngine;

namespace Legato.Unity
{
    internal sealed class JoystickAxisReader : IDisposable
    {
        private readonly NativeJoystickInput _platform;
#if BEAT_SABER_1_29_0
        private Vector2 _axis;
#endif

        internal JoystickAxisReader(NativeJoystickInput platform)
        {
            _platform = platform;
#if BEAT_SABER_1_29_0
            platform.joystickWasCenteredThisFrameEvent += Centered;
            platform.joystickWasNotCenteredThisFrameEvent += Moved;
#endif
        }

        internal Vector2 Value =>
#if LEGATO_BEFORE_MULTIPLAYER
            new Vector2(
                DominantAxis(_platform.HorizontalAxisValue(XRNode.LeftHand), _platform.HorizontalAxisValue(XRNode.RightHand)),
                DominantAxis(_platform.VerticalAxisValue(XRNode.LeftHand), _platform.VerticalAxisValue(XRNode.RightHand)));
#elif BEAT_SABER_1_29_0
            _axis;
#else
            _platform.GetAnyJoystickMaxAxis();
#endif

#if LEGATO_BEFORE_MULTIPLAYER
        private static float DominantAxis(float left, float right) =>
            Mathf.Abs(left) >= Mathf.Abs(right) ? left : right;
#endif

        public void Dispose()
        {
#if BEAT_SABER_1_29_0
            _platform.joystickWasCenteredThisFrameEvent -= Centered;
            _platform.joystickWasNotCenteredThisFrameEvent -= Moved;
#endif
        }

#if BEAT_SABER_1_29_0
        private void Centered() => _axis = Vector2.zero;
        private void Moved(Vector2 axis) => _axis = axis;
#endif
    }
}
#endif
