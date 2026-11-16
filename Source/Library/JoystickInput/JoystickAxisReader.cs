#if !COMPILE_EDITOR
using System;
using UnityEngine;

namespace Legato.Unity
{
    internal sealed class JoystickAxisReader : IDisposable
    {
        private readonly IVRPlatformHelper _platform;
#if BEAT_SABER_1_29_0
        private Vector2 _axis;
#endif

        internal JoystickAxisReader(IVRPlatformHelper platform)
        {
            _platform = platform;
#if BEAT_SABER_1_29_0
            platform.joystickWasCenteredThisFrameEvent += Centered;
            platform.joystickWasNotCenteredThisFrameEvent += Moved;
#endif
        }

        internal Vector2 Value =>
#if BEAT_SABER_1_29_0
            _axis;
#else
            _platform.GetAnyJoystickMaxAxis();
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
