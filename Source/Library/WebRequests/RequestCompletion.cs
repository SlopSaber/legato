using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Legato.Unity {
    internal static class RequestCompletion {
        internal static async Task Send(UnityWebRequest request) {
            var operation = request.SendWebRequest();
            if (operation.isDone) return;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            operation.completed += _ => completion.TrySetResult(true);
            await completion.Task.ConfigureAwait(true);
        }
    }
}
