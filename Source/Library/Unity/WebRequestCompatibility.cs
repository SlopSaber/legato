using UnityEngine.Networking;

namespace Legato.Unity {
    internal static class WebRequestCompatibility {
        internal static bool IsSuccess(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0
            !request.isNetworkError && !request.isHttpError;
#else
            request.result == UnityWebRequest.Result.Success;
#endif
    }
}
