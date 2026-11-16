using UnityEngine.Networking;

namespace Legato.Sira
{
    internal static class NativeAccess
    {
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
