using System.Collections.Generic;
using System.Linq;

namespace Legato.SongBrowser {
    internal static class BrowserLevels {
        internal static IEnumerable<NativeSongPack> Packs(BeatmapLevelsModel model) =>
#if LEGATO_BEFORE_MULTIPLAYER
            model?.allLoadedBeatmapLevelPackCollection?.beatmapLevelPacks ?? System.Array.Empty<NativeSongPack>();
#elif BEAT_SABER_1_29_0
            model.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks;
#else
            model._allLoadedBeatmapLevelsRepository.beatmapLevelPacks;
#endif
        internal static IEnumerable<NativeSongPack> CustomPacks(BeatmapLevelsModel model) =>
#if LEGATO_BEFORE_MULTIPLAYER
            model?.customLevelPackCollection?.beatmapLevelPacks ?? System.Array.Empty<NativeSongPack>();
#elif BEAT_SABER_1_29_0
            model.customLevelPackCollection.beatmapLevelPacks;
#else
            model._customLevelsRepository.beatmapLevelPacks;
#endif
        internal static IEnumerable<BeatmapLevel> Levels(NativeSongPack pack) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            pack.beatmapLevelCollection.beatmapLevels;
#elif BEAT_SABER_1_37_1 && !LEGATO_ADDITIONAL_PACK_LEVELS
            pack.beatmapLevels;
#else
            pack.AllBeatmapLevels();
#endif
        internal static BeatmapLevel Find(BeatmapLevelsModel model, string id) {
#if LEGATO_BEFORE_MULTIPLAYER
            return !string.IsNullOrEmpty(id) && model != null &&
                model._loadedPreviewBeatmapLevels.TryGetValue(id, out var preview) ? preview : null;
#elif BEAT_SABER_1_29_0
            return model.GetLevelPreviewForLevelId(id);
#else
            model._allLoadedBeatmapLevelsRepository.TryGetBeatmapLevelById(id, out var level);
            return level;
#endif
        }
        internal static IEnumerable<string> PlayedIds(PlayerData player) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            foreach(var stats in player.levelsStatsData) if(stats.validScore) yield return stats.levelID;
#else
            foreach(var entry in player.levelsStatsData) if(entry.Value.validScore) yield return entry.Key.levelId;
#endif
        }
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static LevelSelectionFlowCoordinator.State State(SelectLevelCategoryViewController.LevelCategory category, NativeSongPack pack, BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            new LevelSelectionFlowCoordinator.State(category, pack, level, null);
#else
            new LevelSelectionFlowCoordinator.State(category, pack, default(BeatmapKey), level);
#endif
#endif
        internal static float DefaultJumpSpeed(BeatmapDifficulty difficulty) =>
#if LEGATO_VARIABLE_MOVEMENT_DATA
            BeatmapDifficultyMethods.NoteJumpMovementSpeed(difficulty, 0, true);
#else
            BeatmapDifficultyMethods.NoteJumpMovementSpeed(difficulty);
#endif
        internal static int IndexOf(IReadOnlyList<BeatmapLevel> levels, System.Func<BeatmapLevel, bool> predicate) {
            for(int i = 0; i < levels.Count; i++) if(predicate(levels[i])) return i;
            return -1;
        }
        internal static IReadOnlyList<BeatmapLevel> TableLevels(LevelCollectionTableView table) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            table._previewBeatmapLevels;
#else
            table._beatmapLevels;
#endif
        internal static BeatmapLevel DetailLevel(StandardLevelDetailView view) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            view._level;
#else
            view._beatmapLevel;
#endif
        internal static BeatmapKey DetailKey(StandardLevelDetailView view) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static (float noteJumpMovementSpeed, float noteJumpStartBeatOffset) JumpDetails(BeatmapLevel level, BeatmapKey key) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            return (key?.noteJumpMovementSpeed ?? 0, key?.noteJumpStartBeatOffset ?? 0);
#else
            var data = Legato.BeatmapKeyExtensions.GetDifficultyBeatmapData(level, in key);
            return (data?.noteJumpMovementSpeed ?? 0, data?.noteJumpStartBeatOffset ?? 0);
#endif
        }
        internal static string MapperNames(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            level.levelAuthorName;
#else
            level.allMappers.Length == 1 ? level.allMappers[0] : string.Join(" ", level.allMappers);
#endif
    }
}
