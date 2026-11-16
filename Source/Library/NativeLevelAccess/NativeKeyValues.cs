namespace Legato.Beatmaps {
    internal static class NativeKeyValues {
        internal static string LevelId(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key?.level?.levelID ?? string.Empty;
#else
            key.levelId;
#endif
        internal static NativeBeatmapCharacteristic Characteristic(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key.parentDifficultyBeatmapSet.beatmapCharacteristic;
#elif BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            key.characteristic;
#else
            key.beatmapCharacteristic;
#endif
        internal static bool IsValid(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key != null && key.level != null && key.parentDifficultyBeatmapSet != null;
#else
            key.IsValid();
#endif
    }
}
