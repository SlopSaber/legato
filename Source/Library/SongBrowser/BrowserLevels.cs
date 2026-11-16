using System.Collections.Generic;
using System.Linq;

namespace Legato.SongBrowser {
    internal static class BrowserLevels {
        internal static IEnumerable<NativeSongPack> Packs(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0
            model.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks;
#else
            model._allLoadedBeatmapLevelsRepository.beatmapLevelPacks;
#endif
        internal static IEnumerable<NativeSongPack> CustomPacks(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0
            model.customLevelPackCollection.beatmapLevelPacks;
#else
            model._customLevelsRepository.beatmapLevelPacks;
#endif
        internal static IEnumerable<BeatmapLevel> Levels(NativeSongPack pack) =>
#if BEAT_SABER_1_29_0
            pack.beatmapLevelCollection.beatmapLevels;
#else
            pack.AllBeatmapLevels();
#endif
        internal static BeatmapLevel Find(BeatmapLevelsModel model, string id) {
#if BEAT_SABER_1_29_0
            return model.GetLevelPreviewForLevelId(id);
#else
            model._allLoadedBeatmapLevelsRepository.TryGetBeatmapLevelById(id, out var level);
            return level;
#endif
        }
        internal static IEnumerable<string> PlayedIds(PlayerData player) {
#if BEAT_SABER_1_29_0
            foreach(var stats in player.levelsStatsData) if(stats.validScore) yield return stats.levelID;
#else
            foreach(var entry in player.levelsStatsData) if(entry.Value.validScore) yield return entry.Key.levelId;
#endif
        }
        internal static LevelSelectionFlowCoordinator.State State(SelectLevelCategoryViewController.LevelCategory category, NativeSongPack pack, BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            new LevelSelectionFlowCoordinator.State(category, pack, level, null);
#else
            new LevelSelectionFlowCoordinator.State(category, pack, default(BeatmapKey), level);
#endif
        internal static string MapperNames(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level.levelAuthorName;
#else
            level.allMappers.Length == 1 ? level.allMappers[0] : string.Join(" ", level.allMappers);
#endif
    }
}
