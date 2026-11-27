#if LEGATO_WITHOUT_HTTP_CLIENT
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
#endif
using UnityEngine.Networking;
using System.Text;

namespace Legato.Unity {
    internal static class WebRequestCompatibility {
#if LEGATO_WITHOUT_HTTP_CLIENT
        internal static async Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            var request = WebRequest.CreateHttp(url);
            using var abort = cancellationToken.Register(request.Abort);
            try {
                using var response = await request.GetResponseAsync();
                using var stream = response.GetResponseStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, 81920, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return buffer.ToArray();
            } catch (Exception) when (cancellationToken.IsCancellationRequested) {
                throw new OperationCanceledException(cancellationToken);
            }
        }
#endif

        internal static UnityWebRequest PostJson(string url, string json) {
            var request = new UnityWebRequest(url, "POST") {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        internal static bool IsSuccess(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
            !request.isNetworkError && !request.isHttpError;
#else
            request.result == UnityWebRequest.Result.Success;
#endif
        internal static bool IsConnectionError(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
            request.isNetworkError;
#else
            request.result == UnityWebRequest.Result.ConnectionError;
#endif
        internal static bool IsProtocolError(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
            request.isHttpError;
#else
            request.result == UnityWebRequest.Result.ProtocolError;
#endif
    }
}
