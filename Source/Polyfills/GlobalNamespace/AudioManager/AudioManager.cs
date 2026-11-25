#nullable enable

#if LEGATO_DIFFICULTY_BEATMAPS || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || (BEAT_SABER_1_40_0 && !LEGATO_NATIVE_AUDIO_MANAGER)
global using AudioManager = AudioManagerSO;
#endif
