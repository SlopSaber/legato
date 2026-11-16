using HMUI;
using IPA.Utilities;

namespace Legato.Presentation {
    internal static class FlowAccess {
        internal static void Present(FlowCoordinator parent, FlowCoordinator child) =>
            parent.InvokeMethod<object, FlowCoordinator>("PresentFlowCoordinator", child, null,
                ViewController.AnimationDirection.Horizontal, false, false);
        internal static void Dismiss(FlowCoordinator parent, FlowCoordinator child) =>
            parent.InvokeMethod<object, FlowCoordinator>("DismissFlowCoordinator", child,
                ViewController.AnimationDirection.Horizontal, null, false);
    }
}
