using IPA.Utilities;

namespace Legato.UI {
    internal static class NativeLevelSelection {
        internal static LevelSelectionFlowCoordinator.State State(SelectLevelCategoryViewController.LevelCategory category, NativeSongPack pack, BeatmapKey key, BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            new(category, pack, level, key);
#else
            new(category, pack, in key, level);
#endif
        internal static BeatmapLevel ResultsLevel(ResultsViewController view) =>
#if BEAT_SABER_1_29_0
            view._difficultyBeatmap?.level;
#else
            view._beatmapLevel;
#endif
        internal static BeatmapLevel DetailLevel(StandardLevelDetailView view) =>
#if BEAT_SABER_1_29_0
            view.selectedDifficultyBeatmap?.level;
#else
            view._beatmapLevel;
#endif
        internal static BeatmapKey DetailKey(StandardLevelDetailView view) =>
#if BEAT_SABER_1_29_0
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static void RefreshLevelBar(LevelBar bar, StandardLevelDetailView view) {
#if BEAT_SABER_1_29_0
            var key = view.selectedDifficultyBeatmap;
            bar.Setup(key.level, key.parentDifficultyBeatmapSet.beatmapCharacteristic, key.difficulty);
#else
            bar.Setup(view.beatmapKey);
#endif
        }
        internal static void BindLevelBar(LevelBar bar, StandardLevelDetailView view) {
#if !BEAT_SABER_1_29_0
            bar.SetField("_beatmapLevelsModel", view._beatmapLevelsModel);
#endif
        }
    }
}
