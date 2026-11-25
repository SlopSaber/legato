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
        internal static string EnvironmentName(BeatmapLevel level, BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key.GetEnvironmentInfo().environmentName;
#elif BEAT_SABER_1_45_3
            level.GetEnvironmentName(in key);
#else
            level.GetEnvironmentName(Characteristic(key), key.difficulty);
#endif
        internal static string ActiveEnvironmentName(GameplayCoreSceneSetupData scene) =>
#if LEGATO_SINGLE_BEATMAP_ENVIRONMENT
            scene.environmentInfo.environmentName;
#else
            scene.targetEnvironmentInfo.environmentName;
#endif
        internal static bool IsValid(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key != null && key.level != null && key.parentDifficultyBeatmapSet != null;
#else
            key.IsValid();
#endif
    }
}
