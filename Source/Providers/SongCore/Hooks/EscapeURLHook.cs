using System;
using MonoMod.RuntimeDetour;
using UnityEngine.Networking;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// Workaround for <see href="https://github.com/Meivyn/BeatSaberBugs/issues/28"/>.
    /// </summary>
    internal class EscapeURLHook : IInitializable, IDisposable
    {
        private Hook _getEscapedURLForFilePathHook = null!;

        public void Initialize()
        {
            _getEscapedURLForFilePathHook = new Hook(typeof(FileHelpers).GetMethod(nameof(FileHelpers.GetEscapedURLForFilePath))!, GetEscapedURL, true);
        }

        public void Dispose()
        {
            _getEscapedURLForFilePathHook.Dispose();
        }

        private string GetEscapedURL(Func<string, string> original, string filePath)
        {
            return
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
                Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && !uri.IsFile ? filePath
#else
                FileHelpers.PathIsUrl(filePath) ? filePath
#endif
                : $"file:///{UnityWebRequest.EscapeURL(filePath)}";
        }
    }
}
