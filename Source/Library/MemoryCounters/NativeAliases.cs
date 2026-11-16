#if BEAT_SABER_1_29_0
global using MemorySample = Legato.Profiling.UnavailableMemorySample;
#else
global using MemorySample = Unity.Profiling.ProfilerRecorderSample;
#endif
