#if BEAT_SABER_1_29_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
using System;
using System.Runtime.InteropServices;

// Use the same Windows counter in Unity/Mono and the in-process native library.
// Mono's Stopwatch can subtract a process-local origin and rescale its ticks.
namespace Legato.Performance;

internal static class GpuTimingClock
{
    internal static readonly long Frequency = GetFrequency();

    internal static long Timestamp
    {
        get
        {
            if (!QueryPerformanceCounter(out long timestamp))
                throw new InvalidOperationException("Windows performance counter unavailable");
            return timestamp;
        }
    }

    private static long GetFrequency()
    {
        if (!QueryPerformanceFrequency(out long frequency) || frequency <= 0)
            throw new InvalidOperationException("Windows performance counter frequency unavailable");
        return frequency;
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryPerformanceCounter(out long timestamp);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryPerformanceFrequency(out long frequency);
}
#endif
