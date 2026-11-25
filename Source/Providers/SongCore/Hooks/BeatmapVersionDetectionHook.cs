#if !LEGATO_BEFORE_MULTIPLAYER
using System;
using MonoMod.RuntimeDetour;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// This reverses base game logic so it first looks for V3 format instead of V2.
    /// Without this, maps that declare both <c>version</c> and <c>_version</c> will be empty.
    /// It also makes it much faster than the original implementation.
    /// </summary>
    internal class BeatmapVersionDetectionHook : IInitializable, IDisposable
    {
        private const string VersionSearchString = "version";

        private Hook _getVersionHook = null!;

        public void Initialize()
        {
            _getVersionHook = new Hook(typeof(BeatmapSaveDataHelpers).GetMethod(nameof(BeatmapSaveDataHelpers.GetVersion))!, GetVersion, true);
        }

        public void Dispose()
        {
            _getVersionHook.Dispose();
        }

#if BEAT_SABER_1_29_0
        private Version GetVersion(Func<string, Version> original, string data) => ParseVersion(data, Math.Min(50, data.Length)) ?? ParseVersion(data, data.Length) ?? new Version(0, 0, 0);

        private static Version? ParseVersion(string data, int length)
        {
            int index = data.IndexOf(VersionSearchString, 0, length, StringComparison.Ordinal);
            if (index < 0) return null;
            int start = data.IndexOf('"', index + VersionSearchString.Length + 1);
            if (start < 0 || start >= length) return null;
            int end = data.IndexOf('"', start + 1);
            return end >= 0 && end < length && Version.TryParse(data.Substring(start + 1, end - start - 1), out var version) ? version : null;
        }
#else
        private Version GetVersion(Func<string, Version> original, string data)
        {
            return ParseVersion(data.AsSpan(0, Math.Min(50, data.Length))) ?? ParseVersion(data.AsSpan()) ?? new Version(0, 0, 0);
        }

        private Version? ParseVersion(ReadOnlySpan<char> span)
        {
            try
            {
                var index = span.IndexOf(VersionSearchString);
                if (index != -1)
                {
                    span = span.Slice(index + VersionSearchString.Length + 1);
                    span = span.Slice(span.IndexOf('"') + 1);

                    if (Version.TryParse(span.Slice(0, span.IndexOf('"')), out var version))
                    {
                        return version;
                    }
                }
            }
            // Very rare case where the end of the key or value might be out of bounds.
            catch (ArgumentOutOfRangeException) { }

            return null;
        }
#endif
    }
}
#endif
