#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

using System;
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage.Util;
#else
using BeatSaberMarkupLanguage.Util;
#endif

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
