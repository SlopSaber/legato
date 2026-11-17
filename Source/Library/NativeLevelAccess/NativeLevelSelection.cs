using IPA.Utilities;

namespace Legato.UI {
    internal static class NativeLevelSelection {
        internal static System.Reflection.MethodInfo CloseCollectionMethod => typeof(AnnotatedBeatmapLevelCollectionsGridView).GetMethod("CloseLevelCollection",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
        internal static void PlayPreview(LevelCollectionViewController view, BeatmapLevel level, System.Threading.CancellationToken token) {
#if BEAT_SABER_1_29_0
            token.ThrowIfCancellationRequested();
            view.SongPlayerCrossfadeToLevelAsync(level);
#else
            view.SongPlayerCrossfadeToLevelAsync(level, token);
#endif
        }
        internal static bool SelectCharacteristic(BeatmapCharacteristicSegmentedControlController view, NativeBeatmapCharacteristic characteristic) {
            if (view.selectedBeatmapCharacteristic == characteristic) return true;
#if BEAT_SABER_1_29_0
            int index = view._beatmapCharacteristics.IndexOf(characteristic);
#else
            int index = view._currentlyAvailableBeatmapCharacteristics.IndexOf(characteristic);
#endif
            if (index < 0) return false;
            view._segmentedControl.SelectCellWithNumber(index);
#if BEAT_SABER_1_29_0
            view.HandleDifficultySegmentedControlDidSelectCell(view._segmentedControl, index);
#else
            view.HandleBeatmapCharacteristicSegmentedControlDidSelectCell(view._segmentedControl, index);
#endif
            return true;
        }
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
