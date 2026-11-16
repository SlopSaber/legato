using System.Collections.Generic;

namespace Legato.Gameplay {
    internal readonly struct RecordedLevelScore {
        internal readonly string LevelId;
        internal readonly BeatmapDifficulty Difficulty;
        internal readonly string Characteristic;
        internal readonly int HighScore;
        internal readonly bool Valid;

        private RecordedLevelScore(string levelId, BeatmapDifficulty difficulty, string characteristic, int highScore, bool valid) {
            LevelId = levelId; Difficulty = difficulty; Characteristic = characteristic; HighScore = highScore; Valid = valid;
        }

        internal static IEnumerable<RecordedLevelScore> All(PlayerData player) {
#if BEAT_SABER_1_29_0
            foreach (var stats in player.levelsStatsData)
                yield return new RecordedLevelScore(stats.levelID, stats.difficulty, stats.beatmapCharacteristic.serializedName, stats.highScore, stats.validScore);
#else
            foreach (var pair in player.levelsStatsData)
                yield return new RecordedLevelScore(pair.Key.levelId, pair.Key.difficulty, Legato.Beatmaps.NativeLevelAccess.SerializedName(Legato.Beatmaps.NativeKeyValues.Characteristic(pair.Key)), pair.Value.highScore, pair.Value.validScore);
#endif
        }
    }
}
