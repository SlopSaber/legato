using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;

namespace Legato.Shaffuru {
    internal static class NativeAccess {
        internal static BeatmapLevel SceneLevel(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0
            data.difficultyBeatmap.level;
#else
            data.beatmapLevel;
#endif
        internal static void ResetSearch(LevelSearchViewController view, bool notify) {
            if (view == null) return;
#if BEAT_SABER_1_29_0
            view.ResetFilterParams(notify);
#else
            view.ResetAllFilterSettings(notify);
#endif
        }
        internal static IEnumerable<BeatmapLevel> AllLevels(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0
            model._loadedPreviewBeatmapLevels.Values;
#else
            model._allLoadedBeatmapLevelsRepository._idToBeatmapLevel.Values;
#endif
        internal static void UpdatePauseLevel(PauseMenuManager.InitData data, BeatmapLevel level, BeatmapKey key) {
#if BEAT_SABER_1_29_0
            data.SetField("previewBeatmapLevel", level);
            data.SetField("beatmapCharacteristic", key.parentDifficultyBeatmapSet.beatmapCharacteristic);
            data.SetField("beatmapDifficulty", key.difficulty);
#else
            data.SetField("beatmapLevel", level);
            data.SetField("beatmapKey", key);
#endif
        }
    }
}
