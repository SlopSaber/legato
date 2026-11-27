#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER || LEGATO_EARLY_STANDARD_MAPS
global using MemorySample = Legato.Profiling.UnavailableMemorySample;
#else
global using MemorySample = Unity.Profiling.ProfilerRecorderSample;
#endif
