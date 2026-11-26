#nullable enable

using HMUI;
using IPA.Utilities;
using System;
using System.Threading.Tasks;

namespace Legato {
    internal static class FlowCoordinatorExtensions {
        internal static void PresentFlowCoordinator(this FlowCoordinator activeFlow, FlowCoordinator flowCoordinator) {
#if LEGATO_BEFORE_MULTIPLAYER
            activeFlow.InvokeMethod<object, FlowCoordinator>("PresentFlowCoordinator",
                flowCoordinator, null, false, false);
#elif BEAT_SABER_1_29_0
            activeFlow.InvokeMethod<object, FlowCoordinator>("PresentFlowCoordinator",
                flowCoordinator, null, ViewController.AnimationDirection.Horizontal, false, false);
#else
            activeFlow.PresentFlowCoordinator(flowCoordinator);
#endif
        }

        internal static void DismissFlowCoordinator(this FlowCoordinator activeFlow, FlowCoordinator flowCoordinator) {
#if LEGATO_BEFORE_MULTIPLAYER
            activeFlow.InvokeMethod<object, FlowCoordinator>("DismissFlowCoordinator", flowCoordinator, null, false);
#elif BEAT_SABER_1_29_0
            activeFlow.InvokeMethod<object, FlowCoordinator>("DismissFlowCoordinator",
                flowCoordinator, ViewController.AnimationDirection.Horizontal, null, false);
#else
            activeFlow.DismissFlowCoordinator(flowCoordinator);
#endif
        }

        internal static Task DismissView(this FlowCoordinator flowCoordinator, ViewController viewController, Action? finishedCallback = null) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var completion = new TaskCompletionSource<object?>();
            Action callback = () => {
                finishedCallback?.Invoke();
                completion.TrySetResult(null);
            };
#if LEGATO_BEFORE_MULTIPLAYER
            flowCoordinator.InvokeMethod<object, FlowCoordinator>("DismissViewController", viewController, callback, false);
#else
            flowCoordinator.InvokeMethod<object, FlowCoordinator>("DismissViewController",
                viewController, ViewController.AnimationDirection.Horizontal, callback, false);
#endif
            return completion.Task;
#else
            return flowCoordinator.InvokeMethod<Task, FlowCoordinator>("DismissViewController",
                viewController, ViewController.AnimationDirection.Horizontal, finishedCallback, false);
#endif
        }
    }
}
