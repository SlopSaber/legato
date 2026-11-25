namespace Legato.Coroutines {
    internal static class FrameTime {
        internal static float Delta(TimeHelper helper) =>
#if LEGATO_STATIC_TIME_HELPER
            TimeHelper.deltaTime;
#else
            helper.DeltaTime;
#endif
        internal static float FixedDelta(TimeHelper helper) =>
#if LEGATO_STATIC_TIME_HELPER
            TimeHelper.fixedDeltaTime;
#else
            helper.FixedDeltaTime;
#endif
    }
}
