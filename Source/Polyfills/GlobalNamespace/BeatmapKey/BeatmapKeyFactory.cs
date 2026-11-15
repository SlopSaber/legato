#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
namespace Legato {
    internal static class BeatmapKeyFactory {
        internal static BeatmapKey Create(string levelId, BeatmapCharacteristic characteristic, BeatmapDifficulty difficulty) {
#if BEAT_SABER_1_45_3
            return new BeatmapKey(levelId, GameplayType.BeatSaber, characteristic, difficulty);
#else
            return new BeatmapKey(levelId, characteristic, difficulty);
#endif
        }
    }
}
#endif
