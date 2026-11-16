using UnityEngine.Networking;
using System.Text;

namespace Legato.Unity {
    internal static class WebRequestCompatibility {
        internal static UnityWebRequest PostJson(string url, string json) {
            var request = new UnityWebRequest(url, "POST") {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        internal static bool IsSuccess(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0
            !request.isNetworkError && !request.isHttpError;
#else
            request.result == UnityWebRequest.Result.Success;
#endif
        internal static bool IsConnectionError(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0
            request.isNetworkError;
#else
            request.result == UnityWebRequest.Result.ConnectionError;
#endif
        internal static bool IsProtocolError(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0
            request.isHttpError;
#else
            request.result == UnityWebRequest.Result.ProtocolError;
#endif
    }
}
