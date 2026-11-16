using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    internal static class NativeLevelAccess {
        internal static string[] Mappers(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            string.IsNullOrEmpty(level.levelAuthorName) ? Array.Empty<string>() : new[] { level.levelAuthorName };
#else
            level.allMappers;
#endif
        internal static string SerializedName(NativeBeatmapCharacteristic characteristic) =>
#if BEAT_SABER_1_29_0
            characteristic.serializedName;
#else
            characteristic.SerializedName();
#endif
        internal static IEnumerable<BeatmapDifficulty> Difficulties(BeatmapLevel level, NativeBeatmapCharacteristic characteristic) =>
#if BEAT_SABER_1_29_0
            level.previewDifficultyBeatmapSets.Where(set => set.beatmapCharacteristic == characteristic).SelectMany(set => set.beatmapDifficulties);
#elif BEAT_SABER_1_45_3
            level.GetDifficulties(GameplayType.BeatSaber, characteristic);
#else
            level.GetDifficulties(characteristic);
#endif
        internal static BeatmapLevel Preview(BeatmapLevelsModel model, string levelId) =>
#if BEAT_SABER_1_29_0
            model.GetLevelPreviewForLevelId(levelId);
#else
            model.GetBeatmapLevel(levelId);
#endif
        internal static async Task<BeatmapLevel> Load(BeatmapLevelsModel model, string levelId, CancellationToken token) {
#if BEAT_SABER_1_29_0
            var loaded = await model.GetBeatmapLevelAsync(levelId, token);
            if (loaded.isError) throw new InvalidOperationException("Failed to load beatmap level: " + levelId);
            return loaded.beatmapLevel;
#else
            token.ThrowIfCancellationRequested();
            return await Task.FromResult(model.GetBeatmapLevel(levelId));
#endif
        }
        internal static IEnumerable<BeatmapKey> LoadedKeys(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level is IBeatmapLevel full ? full.beatmapLevelData.difficultyBeatmapSets.SelectMany(set => set.difficultyBeatmaps)
                : throw new InvalidOperationException("Load native beatmap data before selecting its difficulty.");
#else
            level.GetBeatmapKeys();
#endif
    }
}
