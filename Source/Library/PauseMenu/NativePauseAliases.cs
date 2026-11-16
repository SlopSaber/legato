#if BEAT_SABER_1_29_0
global using NativePauseLevel = global::IPreviewBeatmapLevel;
global using NativePauseCharacteristic = global::BeatmapCharacteristicSO;
#elif BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
global using NativePauseLevel = global::BeatmapLevel;
global using NativePauseCharacteristic = global::BeatmapCharacteristic;
#else
global using NativePauseLevel = global::BeatmapLevel;
global using NativePauseCharacteristic = global::BeatmapCharacteristicSO;
#endif
