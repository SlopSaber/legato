#nullable enable
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using UnityEngine;

namespace Legato.Presentation {
    internal static class PauseMenuLayout {
        internal static NativePauseLevel? InitializeLevelBar(PauseMenuManager menu) {
            var data = menu != null ? menu._initData : null;
#if BEAT_SABER_1_29_0
            if (data == null || !data.showLevelBar || data.previewBeatmapLevel == null || menu!._levelBar == null || CurrentLevel(menu._levelBar) != null)
                return null;
            menu._levelBar.Setup(data.previewBeatmapLevel, data.beatmapCharacteristic, data.beatmapDifficulty);
            return data.previewBeatmapLevel;
#else
            if (data == null || !data.showLevelBar || data.beatmapLevel == null || menu!._levelBar == null || menu._levelBar._beatmapLevel != null)
                return null;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            menu._levelBar.Setup(data.beatmapLevel, data.beatmapKey.difficulty, data.beatmapKey.characteristic);
#else
            menu._levelBar.Setup(data.beatmapLevel, data.beatmapKey.difficulty, data.beatmapKey.beatmapCharacteristic);
#endif
            return data.beatmapLevel;
#endif
        }

#if BEAT_SABER_1_29_0
        private sealed class LevelIdentity { internal NativePauseLevel? Level; }
        private static readonly ConditionalWeakTable<LevelBar, LevelIdentity> Levels = new();
        internal static void Remember(LevelBar bar, NativePauseLevel level) => Levels.GetOrCreateValue(bar).Level = level;
        internal static NativePauseLevel? CurrentLevel(LevelBar bar) => Levels.TryGetValue(bar, out var value) ? value.Level : null;
        internal static NativePauseLevel? MenuLevel(PauseMenuManager menu) => menu._initData?.previewBeatmapLevel;
        internal static NativePauseLevel? SceneLevel(GameplayCoreSceneSetupData scene) => scene.difficultyBeatmap?.level;
        internal static string Contributors(NativePauseLevel level) => level.levelAuthorName;
        internal static MethodInfo SetupMethod => AccessTools.Method(typeof(LevelBar), "Setup", new[] { typeof(IPreviewBeatmapLevel), typeof(BeatmapCharacteristicSO), typeof(BeatmapDifficulty) });
#else
        internal static NativePauseLevel? CurrentLevel(LevelBar bar) => bar._beatmapLevel;
        internal static NativePauseLevel? MenuLevel(PauseMenuManager menu) => menu._initData?.beatmapLevel;
        internal static NativePauseLevel? SceneLevel(GameplayCoreSceneSetupData scene) => scene.beatmapLevel;
        internal static string Contributors(NativePauseLevel level) => string.Join(", ", (level.allMappers ?? new string[0]).Concat(level.allLighters ?? new string[0]));
        internal static MethodInfo SetupMethod => AccessTools.Method(typeof(LevelBar), "Setup", new[] { typeof(NativePauseLevel), typeof(BeatmapDifficulty), typeof(NativePauseCharacteristic) });
#endif

        internal static PauseMenuManager? GetManager(PauseController controller) =>
            controller.GetField<PauseMenuManager, PauseController>("_pauseMenuManager");

        internal static Transform? FindCanvas(PauseMenuManager manager) =>
            manager.transform.Find("Wrapper/MenuWrapper/Canvas");

        internal static ImageView? FindBackground(Transform canvas) {
            var background = canvas.Find("MainBar/LevelBarSimple/BG");
            return background != null ? background.GetComponent<ImageView>() : null;
        }
    }
}
