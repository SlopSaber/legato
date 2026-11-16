using System;
using System.Collections.Generic;

namespace Legato.Profiling
{
    internal struct MemoryRecorder : IDisposable
    {
#if !BEAT_SABER_1_29_0
        private Unity.Profiling.ProfilerRecorder _recorder;
#endif

        internal static MemoryRecorder Open(string name, int capacity)
        {
#if BEAT_SABER_1_29_0
            return default;
#else
            return new MemoryRecorder
            {
                _recorder = capacity == 0
                    ? new Unity.Profiling.ProfilerRecorder(Unity.Profiling.ProfilerCategory.Memory, name, 0, Unity.Profiling.ProfilerRecorderOptions.None)
                    : Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, name, capacity)
            };
#endif
        }

        internal bool Valid =>
#if BEAT_SABER_1_29_0
            false;
#else
            _recorder.Valid;
#endif

        internal long CurrentValue =>
#if BEAT_SABER_1_29_0
            throw new InvalidOperationException("ProfilerRecorder memory counters are unavailable on this engine.");
#else
            _recorder.CurrentValue;
#endif

        internal static long? AllocationCallstacksEnabled =>
#if BEAT_SABER_1_29_0
            null;
#else
            UnityEngine.Profiling.Profiler.enableAllocationCallstacks ? 1L : 0L;
#endif

        internal void CopyTo(List<MemorySample> samples, bool reset)
        {
#if BEAT_SABER_1_29_0
            throw new InvalidOperationException("ProfilerRecorder frame samples are unavailable on this engine.");
#else
            _recorder.CopyTo(samples, reset);
#endif
        }

        public void Dispose()
        {
#if !BEAT_SABER_1_29_0
            _recorder.Dispose();
#endif
        }
    }

#if BEAT_SABER_1_29_0
    internal readonly struct UnavailableMemorySample
    {
        internal long Value => throw new InvalidOperationException("No frame sample is available.");
    }
#endif
}
