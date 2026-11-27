#if LEGATO_EARLY_STANDARD_MAPS
#nullable enable
namespace Legato.Presentation {
    internal static class FlowAccess {
        internal static void Present(object? parent, EarlyFlowCoordinator child) => child.Present();
        internal static void Dismiss(object? parent, EarlyFlowCoordinator child) => child.Dismiss();
    }
}
#else
using HMUI;
using IPA.Utilities;

namespace Legato.Presentation {
    internal static class FlowAccess {
        internal static void Present(FlowCoordinator parent, FlowCoordinator child) =>
#if LEGATO_HMUI_ACTIVATION_ENUMS
            parent.InvokeMethod<object, FlowCoordinator>("PresentFlowCoordinator", child, null, false, false);
#else
            parent.InvokeMethod<object, FlowCoordinator>("PresentFlowCoordinator", child, null,
                ViewController.AnimationDirection.Horizontal, false, false);
#endif
        internal static void Dismiss(FlowCoordinator parent, FlowCoordinator child) =>
#if LEGATO_HMUI_ACTIVATION_ENUMS
            parent.InvokeMethod<object, FlowCoordinator>("DismissFlowCoordinator", child, null, false);
#else
            parent.InvokeMethod<object, FlowCoordinator>("DismissFlowCoordinator", child,
                ViewController.AnimationDirection.Horizontal, null, false);
#endif
    }
}
#endif
