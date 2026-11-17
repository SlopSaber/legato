using System;
using System.Reflection;
using HMUI;
using UnityEngine.Networking;

namespace Legato.Sira
{
    internal static class NativeAccess
    {
        internal static bool MatchesInstaller(Type expected, Type actual)
        {
#if BEAT_SABER_1_29_0
            if (expected == typeof(AppInit)) return expected.IsAssignableFrom(actual);
#endif
            return expected == actual;
        }

#if BEAT_SABER_1_29_0
        private static readonly Action<FlowCoordinator, ViewController?, ViewController.AnimationType> SetBottomScreen =
            (Action<FlowCoordinator, ViewController?, ViewController.AnimationType>)Delegate.CreateDelegate(
                typeof(Action<FlowCoordinator, ViewController?, ViewController.AnimationType>),
                typeof(FlowCoordinator).GetMethod("SetBottomScreenViewController", BindingFlags.Instance | BindingFlags.NonPublic)!);
#endif

        internal static void SetBottomScreenViewController(FlowCoordinator flow, ViewController? view, ViewController.AnimationType animation)
        {
#if BEAT_SABER_1_29_0
            SetBottomScreen(flow, view, animation);
#else
            flow.SetBottomScreenViewController(view, animation);
#endif
        }

        internal static bool IsPaused(PauseController controller)
        {
#if BEAT_SABER_1_29_0
            return controller._paused;
#else
            return controller._paused == PauseController.PauseState.Paused;
#endif
        }

        internal static float LastMovementTime(Saber saber)
        {
#if BEAT_SABER_1_29_0
            return saber.movementData.lastAddedData.time;
#else
            return saber.movementDataForLogic.lastAddedData.time;
#endif
        }

        internal static bool RequestSucceeded(UnityWebRequest request)
        {
#if BEAT_SABER_1_29_0
            return request.isDone && !request.isNetworkError && !request.isHttpError;
#else
            return request.isDone && request.result == UnityWebRequest.Result.Success;
#endif
        }
    }
}
