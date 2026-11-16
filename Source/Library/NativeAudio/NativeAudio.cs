namespace Legato.Audio {
    internal static class NativeAudio {
        internal static float? MainVolumeFade(AudioManager manager) =>
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0
            null;
#else
            manager.mainVolumeFade;
#endif
    }
}
