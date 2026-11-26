#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Profiler = UnityEngine.Profiling.Profiler;

namespace Legato.Profiling
{
    internal struct MemoryRecorder : IDisposable
    {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private readonly Func<long>? _read;

        private MemoryRecorder(Func<long> read) => _read = read;

        internal static MemoryRecorder Open(string name, int capacity)
        {
            if (capacity != 0) return default;
            return name switch
            {
                "Gfx Used Memory" when Profiler.supported => new MemoryRecorder(Profiler.GetAllocatedMemoryForGraphicsDriver),
                "Texture Memory" => new MemoryRecorder(() => checked((long)Texture.currentTextureMemory)),
                _ => default
            };
        }

        internal bool Valid => _read != null;

        internal long CurrentValue => _read?.Invoke()
            ?? throw new InvalidOperationException("This memory counter is unavailable on this engine.");

        internal void CopyTo(List<MemorySample> samples, bool reset) =>
            throw new InvalidOperationException("ProfilerRecorder frame samples are unavailable on this engine.");

        public void Dispose() { }
#else
        private Unity.Profiling.ProfilerRecorder _recorder;

        internal static MemoryRecorder Open(string name, int capacity)
        {
            return new MemoryRecorder
            {
                _recorder = capacity == 0
                    ? new Unity.Profiling.ProfilerRecorder(Unity.Profiling.ProfilerCategory.Memory, name, 0, Unity.Profiling.ProfilerRecorderOptions.None)
                    : Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, name, capacity)
            };
        }

        internal bool Valid => _recorder.Valid;

        internal long CurrentValue => _recorder.CurrentValue;

        internal void CopyTo(List<MemorySample> samples, bool reset) => _recorder.CopyTo(samples, reset);

        public void Dispose() => _recorder.Dispose();
#endif

        internal static long? AllocationCallstacksEnabled =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            Profiler.supported ? (Profiler.enableAllocationCallstacks ? 1L : 0L) : (long?)null;
#else
            Profiler.enableAllocationCallstacks ? 1L : 0L;
#endif
    }

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
    internal readonly struct UnavailableMemorySample
    {
        internal long Value => throw new InvalidOperationException("No frame sample is available.");
    }
#endif
}
