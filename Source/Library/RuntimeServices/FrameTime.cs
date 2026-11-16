namespace Legato.Coroutines {
    internal static class FrameTime {
        internal static float Delta(TimeHelper helper) =>
#if BEAT_SABER_1_29_0
            helper.deltaTime;
#else
            helper.DeltaTime;
#endif
        internal static float FixedDelta(TimeHelper helper) =>
#if BEAT_SABER_1_29_0
            helper.fixedDeltaTime;
#else
            helper.FixedDeltaTime;
#endif
    }
}
