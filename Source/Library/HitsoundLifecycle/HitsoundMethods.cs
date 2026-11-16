namespace Legato.Audio {
    internal static class HitsoundMethods {
#if BEAT_SABER_1_29_0
        internal const string EffectLateUpdate = "LateUpdate";
#else
        internal const string EffectLateUpdate = "OnLateUpdate";
#endif
    }
}
