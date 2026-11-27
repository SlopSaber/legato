#if LEGATO_BSUTILS_EARLY
using System;
using HarmonyLib;
using Legato.BsUtils;
using BS_Utils.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BS_Utils.Utilities
{
    public partial class BSEvents
    {
        private static bool _returningFromGame;
        private static bool _finishPublished;

        [HarmonyPatch]
        private static class EarlySceneEvents
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(MainGameSceneSetupData), nameof(MainGameSceneSetupData.Init))]
            private static void StandardStarted(MainGameSceneSetupData __instance)
            {
                StartPlay(__instance);
                Gamemode.SetNativeMode(__instance.gameplayMode);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(MenuSceneSetupData), nameof(MenuSceneSetupData.StartTutorial))]
            private static void TutorialStarted(TutorialSceneSetupData ____tutorialSceneSetupData)
                => StartPlay(____tutorialSceneSetupData);

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MainGameSceneSetupData), nameof(MainGameSceneSetupData.Finish))]
            private static void StandardFinished(MainGameSceneSetupData __instance, LevelCompletionResults levelCompletionResults)
            {
                if (_finishPublished) return;
                _finishPublished = true;
                Plugin.TriggerLevelFinishEvent(__instance, levelCompletionResults);
                TriggerLevelFinishEvent(__instance, levelCompletionResults);
                if (levelCompletionResults == null)
                {
                    InvokeSafely(levelQuit, __instance, levelCompletionResults);
                }
                else if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared)
                {
                    InvokeSafely(levelCleared, __instance, levelCompletionResults);
                }
                else if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed)
                {
                    InvokeSafely(levelFailed, __instance, levelCompletionResults);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(TutorialSceneSetupData), nameof(TutorialSceneSetupData.Finish))]
            private static void TutorialFinished(TutorialSceneSetupData __instance, bool completed)
            {
                if (_finishPublished) return;
                _finishPublished = true;
                Plugin.TriggerTutorialFinishEvent(__instance, completed);
                TriggerTutorialFinishEvent(__instance, completed);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RestartLevelController), nameof(RestartLevelController.RestartLevel))]
            private static void StandardRestarted(MainGameSceneSetupData ____mainGameSceneSetupData)
            {
                ScoreSubmission.PauseMenuRestart();
                _finishPublished = true;
                InvokeSafely(levelRestarted, ____mainGameSceneSetupData, null);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RestartTutorialController), nameof(RestartTutorialController.RestartLevel))]
            private static void TutorialRestarted()
            {
                ScoreSubmission.PauseMenuRestart();
                _finishPublished = true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(MenuSceneSetup), "Start")]
            private static void MenuActive() => InvokeSafely(menuSceneActive);

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MenuSceneSetup), "Start")]
            private static void MenuLoaded(MainFlowCoordinator ____mainFlowCoordinator)
            {
                bool fresh = !_returningFromGame;
                _returningFromGame = false;
                Plugin.OnEarlyMenuLoaded();
                Gamemode.Init();
                if (fresh)
                {
                    MenuSceneSetupData setup = NativeAccess.GetMenuSetup(____mainFlowCoordinator);
                    InvokeSafely(earlyMenuSceneLoadedFresh, setup);
                    InvokeSafely(menuSceneLoadedFresh);
                    InvokeSafely(lateMenuSceneLoadedFresh, setup);
                }

                InvokeSafely(menuSceneLoaded);
            }

#if LEGATO_BSUTILS_EARLY_SCENE_LIST
            [HarmonyPrefix]
            [HarmonyPatch(typeof(AsyncScenesLoader), nameof(AsyncScenesLoader.LoadScenes))]
            private static void ObserveGameLoad(AsyncScenesLoader __instance, SceneInfo[] scenes)
            {
                SceneInfo playerScene = Array.Find(scenes, scene =>
                    scene != null && (scene.sceneName == "StandardLevel" || scene.sceneName == "TutorialLevel"));
                AttachLoadHandler(__instance, playerScene, scenes);
            }
#else
            [HarmonyPostfix]
            [HarmonyPatch(typeof(AsyncScenesLoader), "Start")]
            private static void ObserveGameLoad(AsyncScenesLoader __instance, SceneInfo ____mainScene) =>
                AttachLoadHandler(__instance, ____mainScene, __instance.additiveScenes);
#endif

            private static void AttachLoadHandler(AsyncScenesLoader loader, SceneInfo mainScene, SceneInfo[] scenes)
            {
                string sceneName = mainScene != null ? mainScene.sceneName : null;
                if (sceneName != "StandardLevel" && sceneName != "TutorialLevel") return;

                void Loaded()
                {
                    loader.loadingDidFinishEvent -= Loaded;
                    _returningFromGame = true;
                    _finishPublished = false;
                    InvokeSafely(gameSceneActive);
                    SubscribeGameplay(SceneManager.GetSceneByName(sceneName), scenes);
                    InvokeSafely(gameSceneLoaded);
                }

                loader.loadingDidFinishEvent += Loaded;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GamePauseManager), nameof(GamePauseManager.pause), MethodType.Setter)]
            private static void StandardPauseChanged(bool value, bool __state) => NotifyPause(value, __state);

            [HarmonyPostfix]
            [HarmonyPatch(typeof(TutorialPauseManager), nameof(TutorialPauseManager.pause), MethodType.Setter)]
            private static void TutorialPauseChanged(bool value, bool __state) => NotifyPause(value, __state);

            [HarmonyPrefix]
            [HarmonyPatch(typeof(GamePauseManager), nameof(GamePauseManager.pause), MethodType.Setter)]
            private static void CaptureStandardPause(GamePauseManager __instance, out bool __state)
                => __state = __instance.pause;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(TutorialPauseManager), nameof(TutorialPauseManager.pause), MethodType.Setter)]
            private static void CaptureTutorialPause(TutorialPauseManager __instance, out bool __state)
                => __state = __instance.pause;

            private static void NotifyPause(bool value, bool previous)
            {
                if (value != previous) InvokeSafely(value ? songPaused : songUnpaused);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StandardLevelSelectionFlowCoordinator), nameof(StandardLevelSelectionFlowCoordinator.Present))]
            private static void CatalogSelected(StandardLevelSelectionFlowCoordinator __instance,
                StandardLevelSelectionNavigationController ____levelSelectionNavigationController, IStandardLevel[] levels, GameplayMode gameplayMode)
            {
                InvokeSafely(characteristicSelected, __instance, gameplayMode);
                InvokeSafely(levelPackSelected, ____levelSelectionNavigationController, levels);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StandardLevelSelectionFlowCoordinator), "HandleLevelListViewControllerDidSelectLevel")]
            private static void BuiltInLevelSelected(StandardLevelListViewController viewController, IStandardLevel level)
                => InvokeSafely(levelSelected, viewController, level);

#if !LEGATO_BSUTILS_EARLY_SCENE_LIST
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomLevelSelectionFlowCoordinator), "HandleCustomLevelListViewControllerDidSelectSong")]
            private static void CustomLevelSelected(CustomLevelListViewController viewController, IStandardLevel level)
                => InvokeSafely(levelSelected, viewController, level);
#endif

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.SetContent))]
            private static void DifficultyChanged(StandardLevelDetailViewController __instance)
                => InvokeSafely(difficultySelected, __instance);

            private static void StartPlay(GameSceneSetupData setup)
            {
                _finishPublished = false;
                ScoreSubmission._wasDisabled = false;
                ScoreSubmission.LastDisablers = Array.Empty<string>();
                Plugin.scenesTransitionSetupData = setup;
                Plugin.LevelData.GameplayCoreSceneSetupData = setup as MainGameSceneSetupData;
                Plugin.LevelData.IsSet = Plugin.LevelData.GameplayCoreSceneSetupData != null;
                Plugin.LevelData.Mode = Plugin.LevelData.IsSet ? Mode.Standard : Mode.None;
            }
        }

        private static void SubscribeGameplay(Scene scene, SceneInfo[] additiveScenes)
        {
            BeatmapObjectSpawnController spawner = FindInScene<BeatmapObjectSpawnController>(scene);
            if (spawner != null)
            {
                spawner.noteWasCutEvent += (_, note, cut) => InvokeSafely(noteWasCut, note, cut);
                spawner.noteWasMissedEvent += (_, note) => InvokeSafely(noteWasMissed, note);
            }

            ScoreController score = FindInScene<ScoreController>(scene);
            if (score != null)
            {
                score.scoreDidChangeEvent += points => InvokeSafely(scoreDidChange, points);
                score.comboDidChangeEvent += combo =>
                {
                    InvokeSafely(comboDidChange, combo);
                    if (combo == 0) InvokeSafely(comboDidBreak);
                };
                score.multiplierDidChangeEvent += (multiplier, progress) =>
                {
                    InvokeSafely(multiplierDidChange, multiplier, progress);
                    if (multiplier > 1 && progress < 0.1f) InvokeSafely(multiplierDidIncrease, multiplier);
                };
            }

            GameEnergyCounter energy = FindInScene<GameEnergyCounter>(scene);
            if (energy != null)
            {
                energy.gameEnergyDidReach0Event += () => InvokeSafely(energyReachedZero);
                energy.gameEnergyDidChangeEvent += value => InvokeSafely(energyDidChange, value);
            }

            BeatmapObjectCallbackController beatmap = FindInScene<BeatmapObjectCallbackController>(scene);
            if (beatmap != null) beatmap.beatmapEventDidTriggerEvent += value => InvokeSafely(beatmapEvent, value);

            foreach (SceneInfo additive in additiveScenes ?? Array.Empty<SceneInfo>())
            {
                if (additive == null) continue;
                ObstacleSaberSparkleEffectManager sparks =
                    FindInScene<ObstacleSaberSparkleEffectManager>(SceneManager.GetSceneByName(additive.sceneName));
                if (sparks == null) continue;
                sparks.sparkleEffectDidStartEvent += saber => InvokeSafely(sabersStartCollide, saber);
                sparks.sparkleEffectDidEndEvent += saber => InvokeSafely(sabersEndCollide, saber);
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }

            return null;
        }
    }
}
#endif
