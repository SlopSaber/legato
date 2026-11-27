#if LEGATO_SIRA_EARLY_CONTEXTS
using System;
using System.Collections.Generic;
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
        [HarmonyPatch(typeof(MainSystemInit), "Awake")]
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AsyncScenesLoader), "Start")]
        private static void ObserveGameplayLoad(AsyncScenesLoader __instance, SceneInfo ____mainScene)
        {
            if (_reportFailure == null || ____mainScene == null)
            {
                return;
            }

            Location location;
            switch (____mainScene.sceneName)
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
                __instance.loadingDidFinishEvent -= Loaded;
                Run(() => InitializeGameplay(location, SceneManager.GetSceneByName(____mainScene.sceneName)));
            }

            __instance.loadingDidFinishEvent += Loaded;
        }

        private static void InitializeGameplay(Location location, Scene playerScene)
        {
            Scene coreScene = SceneManager.GetSceneByName("GameCore");
            if (!coreScene.isLoaded || !playerScene.isLoaded)
            {
                return;
            }

            EarlySceneContext app = EnsureApp();
            PlayerController? player = Find<PlayerController>(coreScene);
            EarlySceneContext core = Install(Location.GameCore, coreScene, app.Container, container =>
            {
                Bind(container, player);
                Bind(container, Find<AudioTimeSyncController>(playerScene));
                Bind(container, Find<BeatmapDataModel>(playerScene));
                Bind(container, Find<BeatmapObjectCallbackController>(playerScene));
                Bind(container, Find<BeatmapObjectSpawnController>(playerScene));
            });

            Install(location, playerScene, core.Container, container =>
            {
                Bind(container, Find<ScoreController>(playerScene));
                Bind(container, Find<GameplayManager>(playerScene));
                Bind(container, Find<GameEnergyCounter>(playerScene));
                Bind(container, Find<PauseMenuManager>(playerScene));
                Bind(container, Find<MainGameSceneSetup>(playerScene));
                Bind(container, Find<TutorialSceneSetup>(playerScene));
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
                added.Install(location, scene, parent, bindNative);
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
