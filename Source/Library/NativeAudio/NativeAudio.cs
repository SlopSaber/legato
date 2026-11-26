namespace Legato.Audio {
    internal static class NativeAudio {
        internal static float MusicPitch(AudioManager manager) {
            if (manager == null) return 1f;
#if LEGATO_MIXER_MUSIC_PITCH
            return manager._audioMixer.GetFloat("MusicPitch", out var pitch) ? pitch : 1f;
#else
            return manager._musicPitch;
#endif
        }
        internal static float? MainVolumeFade(AudioManager manager) =>
#if LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0
            null;
#else
            manager.mainVolumeFade;
#endif
    }
}
