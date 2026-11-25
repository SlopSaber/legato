using UnityEngine;

namespace Legato.Audio {
    internal static class UiAudioSource {
        internal static AudioSource Current(BasicUIAudioManager manager) =>
#if LEGATO_SINGLE_UI_AUDIO_SOURCE
            manager._audioSource;
#else
            manager._audioSources[manager._index];
#endif
    }
}
