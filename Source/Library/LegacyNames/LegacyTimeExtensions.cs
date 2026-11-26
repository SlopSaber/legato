#if LEGATO_BEFORE_MULTIPLAYER
using System;

namespace Legato.Runtime;

internal static class LegacyTimeExtensions
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static long ToUnixTime(this DateTime time) =>
        (long)(time.ToUniversalTime() - UnixEpoch).TotalSeconds;

    internal static DateTime AsUnixTime(this long seconds) => UnixEpoch.AddSeconds(seconds);
}
#endif
