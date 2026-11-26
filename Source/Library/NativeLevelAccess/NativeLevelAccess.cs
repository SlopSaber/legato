using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Legato.Beatmaps {
    internal static class NativeLevelAccess {
        internal static string[] Mappers(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            string.IsNullOrEmpty(level.levelAuthorName) ? Array.Empty<string>() : new[] { level.levelAuthorName };
#else
            level.allMappers;
#endif
        internal static string SerializedName(NativeBeatmapCharacteristic characteristic) =>
            Legato.Compatibility.CharacteristicCompatibility.SerializedName(characteristic);
        internal static IEnumerable<BeatmapDifficulty> Difficulties(BeatmapLevel level, NativeBeatmapCharacteristic characteristic) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            level.previewDifficultyBeatmapSets.Where(set => set.beatmapCharacteristic == characteristic).SelectMany(set => set.beatmapDifficulties);
#elif BEAT_SABER_1_45_3
            level.GetDifficulties(GameplayType.BeatSaber, characteristic);
#else
            level.GetDifficulties(characteristic);
#endif
        internal static BeatmapLevel Preview(BeatmapLevelsModel model, string levelId) =>
#if LEGATO_BEFORE_MULTIPLAYER
            model._loadedPreviewBeatmapLevels.TryGetValue(levelId, out var level) ? level : null;
#elif BEAT_SABER_1_29_0
            model.GetLevelPreviewForLevelId(levelId);
#else
            model.GetBeatmapLevel(levelId);
#endif
        internal static async Task<BeatmapLevel> Load(BeatmapLevelsModel model, string levelId, CancellationToken token) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var loaded = await model.GetBeatmapLevelAsync(levelId, token).ConfigureAwait(true);
            if (loaded.isError) throw new InvalidOperationException("Failed to load beatmap level: " + levelId);
            return loaded.beatmapLevel;
#else
            token.ThrowIfCancellationRequested();
            return await Task.FromResult(model.GetBeatmapLevel(levelId)).ConfigureAwait(true);
#endif
        }
        internal static IEnumerable<BeatmapKey> LoadedKeys(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            level is IBeatmapLevel full ? full.beatmapLevelData.difficultyBeatmapSets.SelectMany(set => set.difficultyBeatmaps)
                : throw new InvalidOperationException("Load native beatmap data before selecting its difficulty.");
#else
            level.GetBeatmapKeys();
#endif
    }
}
