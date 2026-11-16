using System;

namespace Legato.Performance {
    internal struct CpuFrameTiming : IDisposable {
#if !BEAT_SABER_1_29_0
        private Unity.Profiling.ProfilerRecorder _recorder;
#endif
        internal void Start() {
#if !BEAT_SABER_1_29_0
            _recorder = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "CPU Main Thread Frame Time");
#endif
        }
        internal bool Valid =>
#if BEAT_SABER_1_29_0
            false;
#else
            _recorder.Valid;
#endif
        internal float ReadMilliseconds() {
#if !BEAT_SABER_1_29_0
            if (_recorder.Valid && _recorder.Count > 0) {
                float value = _recorder.LastValue * 0.000001f;
                if (value > 0 && !float.IsNaN(value) && !float.IsInfinity(value)) return value;
            }
#endif
            return float.NaN;
        }
        public void Dispose() {
#if !BEAT_SABER_1_29_0
            _recorder.Dispose();
#endif
        }
    }
}
