#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Legato.Beatmaps {
    internal static class BeatmapLevelCharacteristics {
#if BEAT_SABER_1_29_0
        internal static bool HasDifficulty(BeatmapLevel level, BeatmapCharacteristicSO characteristic, BeatmapDifficulty difficulty) =>
            level.previewDifficultyBeatmapSets.Any(set => set.beatmapCharacteristic == characteristic && set.beatmapDifficulties.Contains(difficulty));
#elif BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        internal static bool HasDifficulty(BeatmapLevel level, BeatmapCharacteristic characteristic, BeatmapDifficulty difficulty) =>
#if BEAT_SABER_1_45_3
            level.GetBeatmapKeys().Any(key => key.characteristic == characteristic && key.difficulty == difficulty);
#else
            level.GetDifficultyBeatmapData(characteristic, difficulty) != null;
#endif
#else
        internal static bool HasDifficulty(BeatmapLevel level, BeatmapCharacteristicSO characteristic, BeatmapDifficulty difficulty) =>
            level.GetDifficultyBeatmapData(characteristic, difficulty) != null;
#endif

#if BEAT_SABER_1_29_0
        internal static IEnumerable<BeatmapCharacteristicSO> GetBeatSaber(BeatmapLevel level) =>
            level.previewDifficultyBeatmapSets.Select(set => set.beatmapCharacteristic);
#elif BEAT_SABER_1_45_3
        internal static IEnumerable<BeatmapCharacteristic> GetBeatSaber(BeatmapLevel level) =>
            level.GetCharacteristics(GameplayType.BeatSaber);
#elif BEAT_SABER_1_45_1
        internal static IEnumerable<BeatmapCharacteristic> GetBeatSaber(BeatmapLevel level) =>
            level.GetCharacteristics();
#else
        internal static IEnumerable<BeatmapCharacteristicSO> GetBeatSaber(BeatmapLevel level) =>
            level.GetCharacteristics();
#endif
    }
}
