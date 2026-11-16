using UnityEngine;

namespace Legato.Audio {
    internal static class UiAudioSource {
        internal static AudioSource Current(BasicUIAudioManager manager) =>
#if BEAT_SABER_1_29_0
            manager._audioSource;
#else
            manager._audioSources[manager._index];
#endif
    }
}
