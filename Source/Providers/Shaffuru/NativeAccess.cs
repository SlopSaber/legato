#if LEGATO_BEFORE_MULTIPLAYER
global using IReadonlyBeatmapData = global::BeatmapData;
#endif
using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;

namespace Legato.Shaffuru {
    internal static class NativeAccess {
        internal static BeatmapLevel SceneLevel(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            data.difficultyBeatmap.level;
#else
            data.beatmapLevel;
#endif
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static void ResetSearch(LevelSearchViewController view, bool notify) {
            if (view == null) return;
#if BEAT_SABER_1_29_0
            view.ResetFilterParams(notify);
#else
            view.ResetAllFilterSettings(notify);
#endif
        }
#endif
        internal static IEnumerable<BeatmapLevel> AllLevels(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            model._loadedPreviewBeatmapLevels.Values;
#else
            model._allLoadedBeatmapLevelsRepository._idToBeatmapLevel.Values;
#endif
        internal static void UpdatePauseLevel(PauseMenuManager.InitData data, BeatmapLevel level, BeatmapKey key) {
#if LEGATO_BEFORE_MULTIPLAYER
            data.SetField("songName", level.songName);
            data.SetField("songSubName", level.songSubName);
            data.SetField("difficultyName", key.difficulty.Name());
#elif BEAT_SABER_1_29_0
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
