using System.Threading;
using System.Threading.Tasks;

namespace Legato.Platform {
    internal static class NativePlatformToken {
        internal static async Task<string> CrossPlatform(PlatformAuthenticationTokenProvider provider, CancellationToken token) {
            token.ThrowIfCancellationRequested();
#if BEAT_SABER_1_29_0
            var result = await provider.GetAuthenticationToken().ConfigureAwait(true);
#else
            var result = await provider.GetXPlatformAccessToken(token).ConfigureAwait(true);
#endif
            token.ThrowIfCancellationRequested();
            return result.token;
        }
    }
}
