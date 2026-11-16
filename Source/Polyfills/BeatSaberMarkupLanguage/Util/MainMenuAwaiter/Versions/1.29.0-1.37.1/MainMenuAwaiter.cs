#nullable enable

#if !LEGATO_BSML_CURRENT
using System;

namespace BeatSaberMarkupLanguage.Util {
    internal static class MainMenuAwaiter {
        internal static event Action MainMenuInitializing {
            add => value();
            remove { }
        }
    }
}
#endif
