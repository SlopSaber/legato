#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
global using BeatmapLevel = global::IPreviewBeatmapLevel;
global using NativeBeatmapLevelPack = global::IBeatmapLevelPack;
#else
global using NativeBeatmapLevelPack = global::BeatmapLevelPack;
#endif
