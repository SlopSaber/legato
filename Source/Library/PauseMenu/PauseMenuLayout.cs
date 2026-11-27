#nullable enable
#if LEGATO_BEFORE_MULTIPLAYER
using TMPro;
using UnityEngine.UI;
#else
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HMUI;
#endif
using IPA.Utilities;
using UnityEngine;

namespace Legato.Presentation {
    internal static class PauseMenuLayout {
#if LEGATO_EARLY_STANDARD_MAPS
        internal static NativePauseLevel? SceneLevel(MainGameSceneSetup scene) =>
            scene.GetField<MainGameSceneSetupData, MainGameSceneSetup>("_mainGameSceneSetupData")?.difficultyLevel?.level;

        internal static (TextMeshProUGUI Title, TextMeshProUGUI Difficulty) LegacyLabels(PauseMenuManager menu) =>
            (menu.GetField<TextMeshProUGUI, PauseMenuManager>("_levelNameText"),
             menu.GetField<TextMeshProUGUI, PauseMenuManager>("_levelDifficultyText"));

        internal static (Button? Back, Button? Restart, Button? Continue) LegacyButtons(PauseMenuManager menu) {
            Button? back = null;
            Button? restart = null;
            Button? resume = null;
            var wrapper = menu.GetField<GameObject, PauseMenuManager>("_gameObjectsWrapper");
            if (wrapper == null) return (null, null, null);
            foreach (Button button in wrapper.GetComponentsInChildren<Button>(true)) {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++) {
                    if (button.onClick.GetPersistentTarget(i) != menu) continue;
                    switch (button.onClick.GetPersistentMethodName(i)) {
                        case nameof(PauseMenuManager.MenuButtonPressed): back = button; break;
                        case nameof(PauseMenuManager.RestartButtonPressed): restart = button; break;
                        case nameof(PauseMenuManager.ContinueButtonPressed): resume = button; break;
                    }
                }
            }
            return (back, restart, resume);
        }
#elif LEGATO_BEFORE_MULTIPLAYER
        internal static NativePauseLevel? SceneLevel(GameplayCoreSceneSetupData scene) => scene.difficultyBeatmap?.level;
        internal static string Contributors(NativePauseLevel level) => level.levelAuthorName;

        internal static (TextMeshProUGUI Title, TextMeshProUGUI Difficulty) LegacyLabels(PauseMenuManager menu) =>
            (menu.GetField<TextMeshProUGUI, PauseMenuManager>("_levelNameText"),
             menu.GetField<TextMeshProUGUI, PauseMenuManager>("_beatmapDifficultyText"));

        internal static (Button Back, Button Restart, Button Continue) LegacyButtons(PauseMenuManager menu) =>
            (menu.GetField<Button, PauseMenuManager>("_backButton"),
             menu.GetField<Button, PauseMenuManager>("_restartButton"),
             menu.GetField<Button, PauseMenuManager>("_continueButton"));
#else
        internal static NativePauseLevel? InitializeLevelBar(PauseMenuManager menu) {
            var data = InitData(menu);
            var bar = menu != null ? menu.GetField<LevelBar, PauseMenuManager>("_levelBar") : null;
#if BEAT_SABER_1_29_0
            if (data == null || !data.showLevelBar || data.previewBeatmapLevel == null || bar == null || CurrentLevel(bar) != null)
                return null;
            bar.Setup(data.previewBeatmapLevel, data.beatmapCharacteristic, data.beatmapDifficulty);
            return data.previewBeatmapLevel;
#else
            if (data == null || !data.showLevelBar || data.beatmapLevel == null || bar == null || CurrentLevel(bar) != null)
                return null;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            bar.Setup(data.beatmapLevel, data.beatmapKey.difficulty, data.beatmapKey.characteristic);
#else
            bar.Setup(data.beatmapLevel, data.beatmapKey.difficulty, data.beatmapKey.beatmapCharacteristic);
#endif
            return data.beatmapLevel;
#endif
        }

#if !LEGATO_LEVEL_BAR_CACHES_LEVEL
        private sealed class LevelIdentity { internal NativePauseLevel? Level; }
        private static readonly ConditionalWeakTable<LevelBar, LevelIdentity> Levels = new();
        internal static void Remember(LevelBar bar, NativePauseLevel? level) => Levels.GetOrCreateValue(bar).Level = level;
        internal static NativePauseLevel? CurrentLevel(LevelBar bar) => Levels.TryGetValue(bar, out var value) ? value.Level : null;
#else
        internal static NativePauseLevel? CurrentLevel(LevelBar bar) => bar.GetField<NativePauseLevel, LevelBar>("_beatmapLevel");
#endif

#if BEAT_SABER_1_29_0
        internal static NativePauseLevel? MenuLevel(PauseMenuManager menu) => InitData(menu)?.previewBeatmapLevel;
        internal static NativePauseLevel? SceneLevel(GameplayCoreSceneSetupData scene) => scene.difficultyBeatmap?.level;
        internal static string Contributors(NativePauseLevel level) => level.levelAuthorName;
        internal static MethodInfo SetupMethod => AccessTools.Method(typeof(LevelBar), "Setup", new[] { typeof(IPreviewBeatmapLevel), typeof(BeatmapCharacteristicSO), typeof(BeatmapDifficulty) });
#else
        internal static NativePauseLevel? MenuLevel(PauseMenuManager menu) => InitData(menu)?.beatmapLevel;
        internal static NativePauseLevel? SceneLevel(GameplayCoreSceneSetupData scene) => scene.beatmapLevel;
        internal static string Contributors(NativePauseLevel level) => string.Join(", ", (level.allMappers ?? new string[0]).Concat(level.allLighters ?? new string[0]));
        internal static MethodInfo SetupMethod => AccessTools.Method(typeof(LevelBar), "Setup", new[] { typeof(NativePauseLevel), typeof(BeatmapDifficulty), typeof(NativePauseCharacteristic) });
#endif

        private static PauseMenuManager.InitData? InitData(PauseMenuManager menu) =>
            menu != null ? menu.GetField<PauseMenuManager.InitData, PauseMenuManager>("_initData") : null;

        internal static Transform? FindCanvas(PauseMenuManager manager) =>
            manager.transform.Find("Wrapper/MenuWrapper/Canvas");
#endif

#if !LEGATO_EARLY_STANDARD_MAPS
        internal static PauseMenuManager? GetManager(PauseController controller) =>
            controller.GetField<PauseMenuManager, PauseController>("_pauseMenuManager");
#endif

        internal static Canvas? ParentCanvas(Component? component) {
            for (Transform? current = component != null ? component.transform : null; current != null; current = current.parent) {
                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null) return canvas;
            }
            return null;
        }

#if !LEGATO_BEFORE_MULTIPLAYER
        internal static ImageView? FindBackground(Transform canvas) {
            var background = canvas.Find("MainBar/LevelBarSimple/BG");
            return background != null ? background.GetComponent<ImageView>() : null;
        }
#endif
    }
}
