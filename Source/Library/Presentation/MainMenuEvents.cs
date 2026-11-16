#nullable enable

using System;
using BeatSaberMarkupLanguage.Util;

namespace Legato.Presentation {
    internal static class MainMenuEvents {
        internal static IDisposable Subscribe(Action callback) => new Subscription(callback);

        private sealed class Subscription : IDisposable {
            private readonly Action _callback;
            private bool _disposed;

            internal Subscription(Action callback) {
                _callback = callback;
                MainMenuAwaiter.MainMenuInitializing += callback;
            }

            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
                MainMenuAwaiter.MainMenuInitializing -= _callback;
            }
        }
    }
}
