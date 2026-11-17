using System;
#if BEAT_SABER_1_29_0
using System.Runtime.InteropServices;
using System.Threading;
#endif

namespace Legato.Performance {
    internal struct CpuFrameTiming : IDisposable {
#if BEAT_SABER_1_29_0
        private bool _valid;
        private int _thread, _frames;
        private ulong _previousCpu;
        private float _windowStart, _milliseconds;
#else
        private global::Unity.Profiling.ProfilerRecorder _recorder;
#endif
        internal void Start() {
#if BEAT_SABER_1_29_0
            _milliseconds = float.NaN;
            _frames = 0;
            _valid = false;
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            _thread = Thread.CurrentThread.ManagedThreadId;
            _valid = ReadCpu(out _previousCpu);
            _windowStart = UnityEngine.Time.realtimeSinceStartup;
#else
            _recorder = global::Unity.Profiling.ProfilerRecorder.StartNew(global::Unity.Profiling.ProfilerCategory.Internal, "CPU Main Thread Frame Time");
#endif
        }
        internal bool Valid =>
#if BEAT_SABER_1_29_0
            _valid;
#else
            _recorder.Valid;
#endif
        internal float ReadMilliseconds() {
#if BEAT_SABER_1_29_0
            if (!_valid || Thread.CurrentThread.ManagedThreadId != _thread) return float.NaN;
            ++_frames;
            float now = UnityEngine.Time.realtimeSinceStartup;
            // Average real thread CPU time over 100 ms to reduce OS accounting quantization.
            if (now - _windowStart < 0.1f) return _milliseconds;
            if (!ReadCpu(out ulong current) || current < _previousCpu) {
                _valid = false;
                return float.NaN;
            }
            _milliseconds = (float)((current - _previousCpu) * 0.0001 / _frames);
            _previousCpu = current;
            _frames = 0;
            _windowStart = now;
            return _milliseconds;
#else
            if (_recorder.Valid && _recorder.Count > 0) {
                float value = _recorder.LastValue * 0.000001f;
                if (value > 0 && !float.IsNaN(value) && !float.IsInfinity(value)) return value;
            }
            return float.NaN;
#endif
        }
        public void Dispose() {
#if BEAT_SABER_1_29_0
            _valid = false;
#else
            _recorder.Dispose();
#endif
        }
        internal static string Label =>
#if BEAT_SABER_1_29_0
            "CPU main";
#else
            "CPU";
#endif
#if BEAT_SABER_1_29_0
        private static bool ReadCpu(out ulong time) {
            bool success = GetThreadTimes(new IntPtr(-2), out _, out _, out ulong kernel, out ulong user);
            time = kernel + user;
            return success;
        }
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetThreadTimes(IntPtr thread, out ulong created, out ulong exited, out ulong kernel, out ulong user);
#endif
    }
}
