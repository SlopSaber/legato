using System.Collections.Generic;
using System.Linq;

namespace Legato.Beatmaps {
    internal static class LevelDifficulties {
        internal static IEnumerable<BeatmapDifficulty> Standard(BeatmapLevel level) {
#if BEAT_SABER_1_29_0
            return level.previewDifficultyBeatmapSets.Where(set => set.beatmapCharacteristic.serializedName == "Standard")
                .SelectMany(set => set.difficulties);
#else
            return StandardKeys(level).Select(key => key.difficulty);
#endif
        }
        internal static IEnumerable<BeatmapKey> StandardKeys(BeatmapLevel level) {
#if BEAT_SABER_1_29_0
            return ((IBeatmapLevel)level).beatmapLevelData.difficultyBeatmapSets
                .Where(set => set.beatmapCharacteristic.serializedName == "Standard")
                .SelectMany(set => set.difficultyBeatmaps);
#else
            return level.GetBeatmapKeys().Where(key => key.gameplayType == GameplayType.BeatSaber && key.characteristic == BeatmapCharacteristic.Standard);
#endif
        }
    }
}
