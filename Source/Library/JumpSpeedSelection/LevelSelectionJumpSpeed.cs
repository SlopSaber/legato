using IPA.Utilities;

namespace Legato.Gameplay {
    internal static class LevelSelectionJumpSpeed {
        internal static float Selected(StandardLevelDetailView view) {
            if (view == null) return 0f;
#if BEAT_SABER_1_29_0
            IDifficultyBeatmap data = view.selectedDifficultyBeatmap;
            if (data == null) return 0f;
            return data.noteJumpMovementSpeed > 0f ? data.noteJumpMovementSpeed
                : BeatmapDifficultyMethods.NoteJumpMovementSpeed(data.difficulty);
#else
            BeatmapLevel level = view.GetField<BeatmapLevel, StandardLevelDetailView>("_beatmapLevel");
            var characteristics = view.GetField<BeatmapCharacteristicSegmentedControlController, StandardLevelDetailView>("_beatmapCharacteristicSegmentedControlController");
            var difficulties = view.GetField<BeatmapDifficultySegmentedControlController, StandardLevelDetailView>("_beatmapDifficultySegmentedControlController");
            if (level == null || characteristics == null || difficulties == null) return 0f;
            var characteristic = characteristics.selectedBeatmapCharacteristic;
            BeatmapDifficulty difficulty = difficulties.selectedDifficulty;
            var key = BeatmapKeyFactory.Create(level.levelID, characteristic, difficulty);
            var data = BeatmapKeyExtensions.GetDifficultyBeatmapData(level, in key);
            return data == null ? 0f : BeatmapDifficultyMethods.NoteJumpMovementSpeed(difficulty, data.noteJumpMovementSpeed, false);
#endif
        }
    }
}
