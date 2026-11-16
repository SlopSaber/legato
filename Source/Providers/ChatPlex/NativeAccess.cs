namespace Legato.ChatPlex {
    internal static class NativeAccess {
        internal static BeatmapCharacteristic Characteristic(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key.parentDifficultyBeatmapSet.beatmapCharacteristic;
#elif BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            key.beatmapCharacteristic;
#else
            key.characteristic;
#endif
    }
}
