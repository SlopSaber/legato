using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Legato.IO {
    internal static class AsyncFiles {
        internal static async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken token = default) {
#if BEAT_SABER_1_29_0
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
            await stream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
#else
            await File.WriteAllBytesAsync(path, bytes, token).ConfigureAwait(false);
#endif
        }
        internal static Task WriteAllTextAsync(string path, string text, CancellationToken token = default) =>
#if BEAT_SABER_1_29_0
            WriteAllBytesAsync(path, new UTF8Encoding(false).GetBytes(text), token);
#else
            File.WriteAllTextAsync(path, text, token);
#endif
    }
}
