using UnityEngine.XR;

namespace Legato.Unity {
    internal sealed class TriggerInput {
#if BEAT_SABER_1_29_0
        private readonly VRControllersInputManager _input;
        internal TriggerInput(VRControllersInputManager input) => _input = input;
        internal float Get(XRNode node) => _input.TriggerValue(node);
#else
        private readonly IVRPlatformHelper _input;
        internal TriggerInput(IVRPlatformHelper input) => _input = input;
        internal float Get(XRNode node) => _input.GetTriggerValue(node);
#endif
    }
}
