using System;
using UnityEngine;
using UnityEngine.XR;

namespace Legato.Performance {
    internal sealed class GpuFrameTiming : IDisposable {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private readonly NativeGpuTiming _native;
#else
        private readonly FrameTiming[] _timings = new FrameTiming[1];
        private ulong _lastFrameTimestamp;
        private bool _reportedInvalidXrGpuTime;
        private readonly Action<string> _debug;
#endif

        internal GpuFrameTiming(Action<string> debug, Action<string> warn) {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            _native = new NativeGpuTiming(debug, warn);
#else
            _debug = debug;
#endif
        }

        internal static bool UsesNativeRenderTiming {
            get {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
                return true;
#else
                return false;
#endif
            }
        }

        internal static bool RequiresLegacyOpenXrHooks =>
#if BEAT_SABER_1_29_0
            false;
#else
            !UsesNativeRenderTiming;
#endif

        internal float ReadEngineTiming() {
#if !BEAT_SABER_1_29_0 && !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
#if !LEGATO_LEGACY_FRAME_TIMING
            if (!UnityEngine.FrameTimingManager.IsFeatureEnabled()) return float.NaN;
#endif
            UnityEngine.FrameTimingManager.CaptureFrameTimings();
            if (UnityEngine.FrameTimingManager.GetLatestTimings(1, _timings) > 0 && FrameTimestamp(_timings[0]) != _lastFrameTimestamp) {
                FrameTiming timing = _timings[0];
                _lastFrameTimestamp = FrameTimestamp(timing);
                float gpu = (float)timing.gpuFrameTime;
                if (Valid(gpu)) return gpu;
            }
#endif
            return float.NaN;
        }

        internal float ReadNativeTiming(MonoBehaviour owner, float current) {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return _native.Read(owner, Time.realtimeSinceStartup);
#else
            return current;
#endif
        }

        internal float ReadXrFallback(float current, XRDisplaySubsystem display, float frameSeconds) {
#if !BEAT_SABER_1_29_0 && !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
            if (!Finite(current) && display.TryGetAppGPUTimeLastFrame(out float seconds) && Valid(seconds)) {
                if (seconds <= Mathf.Max(0.5f, frameSeconds)) return seconds * 1000f;
                if (!_reportedInvalidXrGpuTime) {
                    _debug($"Ignoring inconsistent XR GPU frame time: {seconds}s for a {frameSeconds}s frame");
                    _reportedInvalidXrGpuTime = true;
                }
            }
#endif
            return current;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
#if !BEAT_SABER_1_29_0 && !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
        private static ulong FrameTimestamp(FrameTiming timing) =>
#if LEGATO_LEGACY_FRAME_TIMING
            timing.cpuTimeFrameComplete;
#else
            timing.frameStartTimestamp;
#endif
#endif
        private static bool Valid(float value) => Finite(value) && value > 0f;

        public void Dispose() {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            _native.Dispose();
#endif
        }
    }
}
