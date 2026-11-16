using System.Collections.Generic;
using System.Linq;

namespace Legato.Beatmaps {
    internal static class LevelDifficulties {
        internal static IEnumerable<BeatmapDifficulty> Standard(BeatmapLevel level) {
#if BEAT_SABER_1_29_0
            return level.previewDifficultyBeatmapSets.Where(set => string.Equals(set.beatmapCharacteristic.serializedName, "Standard", System.StringComparison.Ordinal))
                .SelectMany(set => set.beatmapDifficulties);
#else
            return StandardKeys(level).Select(key => key.difficulty);
#endif
        }
        internal static IEnumerable<BeatmapKey> StandardKeys(BeatmapLevel level) {
#if BEAT_SABER_1_29_0
            return ((IBeatmapLevel)level).beatmapLevelData.difficultyBeatmapSets
                .Where(set => string.Equals(set.beatmapCharacteristic.serializedName, "Standard", System.StringComparison.Ordinal))
                .SelectMany(set => set.difficultyBeatmaps);
#elif BEAT_SABER_1_45_3
            return level.GetBeatmapKeys().Where(key => key.gameplayType == GameplayType.BeatSaber && key.characteristic == BeatmapCharacteristic.Standard);
#else
            return level.beatmapBasicData.Keys
                .Where(pair =>
#if BEAT_SABER_1_45_1
                    pair.Item1 == BeatmapCharacteristic.Standard)
#else
                    pair.Item1.serializedName == "Standard")
#endif
                .Select(pair => new BeatmapKey(level.levelID, pair.Item1, pair.Item2));
#endif
        }
    }
}
