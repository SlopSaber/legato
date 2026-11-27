#if LEGATO_SIRA_EARLY_CONTEXTS
using System;
using System.Collections.Generic;
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
using System.Reflection;
using System.Reflection.Emit;
#endif
using HarmonyLib;
using SiraUtil.Zenject;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace Legato.Sira
{
    [HarmonyPatch]
    internal static class EarlySceneBootstrap
    {
        private static readonly List<EarlySceneContext> _contexts = new();
        private static readonly AccessTools.FieldRef<MainGameSceneSetup, MainGameSceneSetupData> StandardData =
            AccessTools.FieldRefAccess<MainGameSceneSetup, MainGameSceneSetupData>("_mainGameSceneSetupData");
        private static readonly AccessTools.FieldRef<TutorialSceneSetup, TutorialSceneSetupData> TutorialData =
            AccessTools.FieldRefAccess<TutorialSceneSetup, TutorialSceneSetupData>("_tutorialSceneSetupData");
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
        private static readonly AccessTools.FieldRef<MainGameSceneSetup, BeatmapDataModel> StandardChart =
            AccessTools.FieldRefAccess<MainGameSceneSetup, BeatmapDataModel>("_beatmapDataModel");
#endif
        private static Action<Exception>? _reportFailure;
        private static EarlySceneContext? _app;

        internal static void Enable(Action<Exception> reportFailure)
        {
            _reportFailure = reportFailure;
            SceneManager.sceneUnloaded += SceneUnloaded;
        }

        internal static void Disable()
        {
            SceneManager.sceneUnloaded -= SceneUnloaded;
            _reportFailure = null;
            for (int index = _contexts.Count - 1; index >= 0; index--)
            {
                if (_contexts[index] != null)
                {
                    _contexts[index].gameObject.SetActive(false);
                    Object.Destroy(_contexts[index].gameObject);
                }
            }

            _contexts.Clear();
            _app = null;
        }

        [HarmonyPostfix]
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
        [HarmonyPatch(typeof(MainSystemInit), "Start")]
#else
        [HarmonyPatch(typeof(MainSystemInit), "Awake")]
#endif
        private static void InitializeApplication(MainSettingsModel ____mainSettingsModel)
        {
            Run(() => EnsureApp(____mainSettingsModel));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuSceneSetup), "Start")]
        private static void InitializeMenu(MenuSceneSetup __instance, MainFlowCoordinator ____mainFlowCoordinator)
        {
            Run(() =>
            {
                Scene scene = __instance.gameObject.scene;
                EarlySceneContext app = EnsureApp();
                Install(Location.Menu, scene, app.Container, container =>
                {
                    Bind(container, ____mainFlowCoordinator);
                    Bind(container, Find<MenuSceneSetup>(scene));
                });
            });
        }

#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AsyncScenesLoader), nameof(AsyncScenesLoader.LoadScenes))]
        private static void ObserveGameplayLoad(AsyncScenesLoader __instance, SceneInfo[] scenes)
        {
            SceneInfo? playerScene = Array.Find(scenes, scene =>
                scene != null && (scene.sceneName == "StandardLevel" || scene.sceneName == "TutorialLevel"));
            AttachGameplayLoadHandler(__instance, playerScene);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MainGameSceneSetup), "Start")]
        private static IEnumerable<CodeInstruction> InstallPlayerWithNativeChart(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo setChart = AccessTools.PropertySetter(typeof(BeatmapDataModel), nameof(BeatmapDataModel.beatmapData));
            MethodInfo install = AccessTools.DeclaredMethod(typeof(EarlySceneBootstrap), nameof(InitializeStandardPlayer));
            bool installed = false;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(setChart))
                {
                    // Bind the exact transformed chart before the game initializes callbacks or audio.
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, install);
                    installed = true;
                }
            }

            if (!installed)
            {
                throw new InvalidOperationException("Could not locate the native chart assignment in MainGameSceneSetup.Start.");
            }
        }

        private static void InitializeStandardPlayer(MainGameSceneSetup setup) =>
            Run(() => InitializeGameplay(Location.StandardPlayer, setup.gameObject.scene));
#else
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AsyncScenesLoader), "Start")]
        private static void ObserveGameplayLoad(AsyncScenesLoader __instance, SceneInfo ____mainScene) =>
            AttachGameplayLoadHandler(__instance, ____mainScene);
#endif

        private static void AttachGameplayLoadHandler(AsyncScenesLoader loader, SceneInfo? mainScene)
        {
            if (_reportFailure == null || mainScene == null)
            {
                return;
            }

            Location location;
            string sceneName = mainScene.sceneName;
            switch (sceneName)
            {
                case "StandardLevel":
                    location = Location.StandardPlayer;
                    break;
                case "TutorialLevel":
                    location = Location.Tutorial;
                    break;
                default:
                    return;
            }

            void Loaded()
            {
                loader.loadingDidFinishEvent -= Loaded;
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
                if (location == Location.StandardPlayer)
                {
                    Run(() => InitializeGameplayCore(SceneManager.GetSceneByName(sceneName)));
                    return;
                }
#endif
                Run(() => InitializeGameplay(location, SceneManager.GetSceneByName(sceneName)));
            }

            loader.loadingDidFinishEvent += Loaded;
        }

        private static EarlySceneContext? InitializeGameplayCore(Scene playerScene)
        {
            Scene coreScene = SceneManager.GetSceneByName("GameCore");
            if (!coreScene.isLoaded || !playerScene.isLoaded)
            {
                return null;
            }

            EarlySceneContext app = EnsureApp();
            PlayerController? player = Find<PlayerController>(coreScene);
            return Install(Location.GameCore, coreScene, app.Container, container =>
            {
                Bind(container, player);
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
                Bind(container, Find<SaberClashEffect>(coreScene));
#endif
                Bind(container, EarlySaberNative.GetColors(player?.leftSaber) ?? EarlySaberNative.GetColors(player?.rightSaber));
                Bind(container, Find<AudioTimeSyncController>(playerScene));
                Bind(container, Find<BeatmapDataModel>(playerScene));
                Bind(container, Find<BeatmapObjectCallbackController>(playerScene));
                Bind(container, Find<BeatmapObjectSpawnController>(playerScene));
            });
        }

        private static void InitializeGameplay(Location location, Scene playerScene)
        {
            EarlySceneContext? core = InitializeGameplayCore(playerScene);
            if (core == null)
            {
                return;
            }

            Install(location, playerScene, core.Container, container =>
            {
                Bind(container, Find<ScoreController>(playerScene));
                Bind(container, Find<GameplayManager>(playerScene));
                Bind(container, Find<GameEnergyCounter>(playerScene));
                Bind(container, Find<PauseMenuManager>(playerScene));
                Bind(container, Find<GamePauseManager>(playerScene));
                Bind(container, Find<TutorialPauseManager>(playerScene));
                Bind(container, Find<TutorialController>(playerScene));
                MainGameSceneSetup? standard = Find<MainGameSceneSetup>(playerScene);
                TutorialSceneSetup? tutorial = Find<TutorialSceneSetup>(playerScene);
                Bind(container, standard);
                Bind(container, tutorial);
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
                if (standard != null)
                {
                    Bind(container, StandardData(standard));
                    BeatmapData? chart = StandardChart(standard)?.beatmapData;
                    if (chart != null)
                    {
                        container.BindInstance(chart);
                    }

                    Bind(container, Find<GameSongController>(playerScene));
                }
#endif
                GameSceneSetupData? setup = standard != null ? StandardData(standard) : tutorial != null ? TutorialData(tutorial) : null;
                Bind(container, setup?.sceneInfo?.gameScenesManager);
            });
        }

        private static EarlySceneContext EnsureApp(MainSettingsModel? settings = null)
        {
            if (_app != null)
            {
                return _app;
            }

            return _app = Install(Location.App, default, null, container => Bind(container, settings));
        }

        private static EarlySceneContext Install(Location location, Scene scene, DiContainer? parent, Action<DiContainer> bindNative)
        {
            foreach (EarlySceneContext context in _contexts)
            {
                if (context != null && context.Location == location && (location == Location.App || context.Scene == scene))
                {
                    return context;
                }
            }

            GameObject root = new(location == Location.App ? "AppCoreSceneContext" : $"SiraUtil {location}");
            if (location == Location.App)
            {
                Object.DontDestroyOnLoad(root);
            }
            else
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            EarlySceneContext added = root.AddComponent<EarlySceneContext>();
            try
            {
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
                DiContainer? nativeContainer = scene.isLoaded
                    ? Find<SceneContext>(scene)?.Container ?? Find<SceneDecoratorContext>(scene)?.Container
                    : null;
                added.Install(location, scene, parent, bindNative, nativeContainer);
#else
                added.Install(location, scene, parent, bindNative);
#endif
                _contexts.Add(added);
                return added;
            }
            catch
            {
                root.SetActive(false);
                Object.Destroy(root);
                throw;
            }
        }

        private static void SceneUnloaded(Scene scene)
        {
            _contexts.RemoveAll(context => context == null || (context.Location != Location.App && context.Scene == scene));
        }

        private static T? Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static void Bind<T>(DiContainer container, T? service) where T : Object
        {
            if (service != null)
            {
                container.BindInstance(service);
            }
        }

        private static void Run(Action action)
        {
            Action<Exception>? reportFailure = _reportFailure;
            if (reportFailure == null)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                reportFailure(exception);
            }
        }
    }
}
#endif
