using IPA.Utilities;

namespace Legato.Gameplay {
    internal static class LevelSelectionJumpSpeed {
        internal static float Selected(StandardLevelDetailView view) {
            if (view == null) return 0f;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            return JumpSpeed(view.selectedDifficultyBeatmap);
#else
            BeatmapLevel level = view.GetField<BeatmapLevel, StandardLevelDetailView>("_beatmapLevel");
            var key = view.beatmapKey;
            if (level == null || !BeatmapKeyExtensions.IsValidKey(key)) return 0f;
            var data = BeatmapKeyExtensions.GetDifficultyBeatmapData(level, in key);
            if (data == null) return 0f;
#if LEGATO_VARIABLE_MOVEMENT_DATA
            return BeatmapDifficultyMethods.NoteJumpMovementSpeed(key.difficulty, data.noteJumpMovementSpeed, false);
#else
            return data.noteJumpMovementSpeed > 0f ? data.noteJumpMovementSpeed
                : BeatmapDifficultyMethods.NoteJumpMovementSpeed(key.difficulty);
#endif
#endif
        }

#if LEGATO_BEFORE_MULTIPLAYER
        internal static float Selected(MissionLevelDetailViewController view) {
            var mission = view.missionNode?.missionData;
            if (mission == null) return 0f;
            if (mission.gameplayModifiers.fastNotes) return 20f;
            return JumpSpeed(mission.level.beatmapLevelData.GetDifficultyBeatmap(mission.beatmapCharacteristic, mission.beatmapDifficulty));
        }
#endif

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private static float JumpSpeed(IDifficultyBeatmap data) {
            if (data == null) return 0f;
            return data.noteJumpMovementSpeed > 0f ? data.noteJumpMovementSpeed
                : BeatmapDifficultyMethods.NoteJumpMovementSpeed(data.difficulty);
        }
#endif
    }
}
