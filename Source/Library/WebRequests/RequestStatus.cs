using UnityEngine.Networking;

namespace Legato.Unity
{
    internal static class RequestStatus
    {
        internal static bool Succeeded(UnityWebRequest request)
        {
#if BEAT_SABER_1_29_0
            return request.isDone && !request.isNetworkError && !request.isHttpError;
#else
            return request.result == UnityWebRequest.Result.Success;
#endif
        }

        internal static string Describe(UnityWebRequest request)
        {
#if BEAT_SABER_1_29_0
            return !request.isDone ? "InProgress" : request.isNetworkError ? "ConnectionError" :
                request.isHttpError ? "ProtocolError" : "Success";
#else
            return request.result.ToString();
#endif
        }
    }
}
