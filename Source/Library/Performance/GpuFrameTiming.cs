using System;
using UnityEngine;
using UnityEngine.XR;

namespace Legato.Performance {
    internal sealed class GpuFrameTiming : IDisposable {
#if LEGATO_NATIVE_GPU_TIMING
        private readonly NativeGpuTiming _native;
#else
        private readonly FrameTiming[] _timings = new FrameTiming[1];
        private ulong _lastFrameTimestamp;
        private bool _reportedInvalidXrGpuTime;
        private readonly Action<string> _debug;
#endif

        internal GpuFrameTiming(Action<string> debug, Action<string> warn) {
#if LEGATO_NATIVE_GPU_TIMING
            _native = new NativeGpuTiming(debug, warn);
#else
            _debug = debug;
#endif
        }

        internal static bool UsesNativeRenderTiming {
            get {
#if LEGATO_NATIVE_GPU_TIMING
                return true;
#else
                return false;
#endif
            }
        }

        internal static bool RequiresLegacyOpenXrHooks => !UsesNativeRenderTiming;

        internal float ReadEngineTiming() {
#if !LEGATO_NATIVE_GPU_TIMING
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
#if LEGATO_NATIVE_GPU_TIMING
            return _native.Read(owner, Time.realtimeSinceStartup);
#else
            return current;
#endif
        }

#if LEGATO_EARLY_STANDARD_MAPS
        internal float ReadLegacyXrFallback(float current, float frameSeconds) {
            if (!Finite(current) && XRStats.TryGetGPUTimeLastFrame(out float seconds) &&
                Valid(seconds) && seconds <= Mathf.Max(0.5f, frameSeconds)) return seconds * 1000f;
            return current;
        }
#else
        internal float ReadXrFallback(float current, XRDisplaySubsystem display, float frameSeconds) {
#if !LEGATO_NATIVE_GPU_TIMING
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
#endif

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
#if !LEGATO_NATIVE_GPU_TIMING
        private static ulong FrameTimestamp(FrameTiming timing) =>
#if LEGATO_LEGACY_FRAME_TIMING
            timing.cpuTimeFrameComplete;
#else
            timing.frameStartTimestamp;
#endif
#endif
        private static bool Valid(float value) => Finite(value) && value > 0f;

        public void Dispose() {
#if LEGATO_NATIVE_GPU_TIMING
            _native.Dispose();
#endif
        }
    }
}
