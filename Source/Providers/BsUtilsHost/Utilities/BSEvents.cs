using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if !LEGATO_BSUTILS_EARLY
using Zenject;
using static GameScenesManager;
#endif
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BS_Utils.Utilities.Events;

namespace BS_Utils.Utilities
{
    public partial class BSEvents : MonoBehaviour
    {
        static BSEvents Instance;

        //Scene Events
        public static event Action menuSceneActive;
        public static event Action menuSceneLoaded;
        /// <summary>
        /// Raised after the game's menu is loaded fresh. This event should be used for cloning game objects. Do NOT modify base game objects during this event.
        /// </summary>
        public static event Action<ScenesTransitionSetupData> earlyMenuSceneLoadedFresh;
        [Obsolete("Use earlyMenuSceneLoadedFresh or lateMenuSceneLoadedFresh.")]
        public static event Action menuSceneLoadedFresh;
        /// <summary>
        /// Raised after the game's menu is loaded fresh and <see cref="earlyMenuSceneLoadedFresh"/> and <see cref="menuSceneLoadedFresh"/> have run.
        /// Base game objects are not guaranteed to be unmodified during this event.
        /// </summary>
        public static event Action<ScenesTransitionSetupData> lateMenuSceneLoadedFresh;
        public static event Action gameSceneActive;
        public static event Action gameSceneLoaded;

        // Menu Events
        public static event Action<StandardLevelDetailViewController> difficultySelected;
#if LEGATO_BSUTILS_EARLY
        public static event Action<StandardLevelSelectionFlowCoordinator, GameplayMode> characteristicSelected;
        public static event Action<StandardLevelSelectionNavigationController, IStandardLevel[]> levelPackSelected;
        public static event Action<VRUI.VRUIViewController, IStandardLevel> levelSelected;
#else
        public static event Action<BeatmapCharacteristicSegmentedControlController, BeatmapCharacteristicSO> characteristicSelected;
        public static event Action<LevelSelectionNavigationController, NativeBeatmapLevelPack> levelPackSelected;
        public static event Action<LevelCollectionViewController, BeatmapLevel> levelSelected;
#endif

        // Game Events
        public static event Action songPaused;
        public static event Action songUnpaused;
        public static event EventHandler<LevelFinishedEventArgs> LevelFinished;
        public static event Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults> levelCleared;
        public static event Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults> levelQuit;
        public static event Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults> levelFailed;
        public static event Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults> levelRestarted;

        public static event Action<NoteController, NoteCutInfo> noteWasCut;
        public static event Action<NoteController> noteWasMissed;
        public static event Action<int, float> multiplierDidChange;
        public static event Action<int> multiplierDidIncrease;
        public static event Action<int> comboDidChange;
        public static event Action comboDidBreak;
        public static event Action<int> scoreDidChange;
        public static event Action<float> energyDidChange;
        public static event Action energyReachedZero;

        public static event Action<BeatmapEventData> beatmapEvent;

        public static event Action<SaberType> sabersStartCollide;
        public static event Action<SaberType> sabersEndCollide;

#if !LEGATO_BEFORE_MULTIPLAYER
        public static event EventHandler<(MultiplayerLevelScenesTransitionSetupData, DisconnectedReason)> MultiplayerDidDisconnect;
#endif

#if !LEGATO_BSUTILS_EARLY
        readonly string[] MainSceneNames = { SceneNames.Game, SceneNames.Credits, SceneNames.BeatmapEditor };
        private bool lastMainSceneWasNotMenu = false;
        GameScenesManager gameScenesManager;
#if !LEGATO_BEFORE_MULTIPLAYER
        private MultiplayerController _multiplayerController;
        private Action<MultiplayerController.State> _multiplayerStateHandler;
#endif
#endif

        // Weak keys allow old subscriber lists and their scene objects to be collected.
        private static readonly ConditionalWeakTable<Delegate, Delegate[]> InvocationLists = new ConditionalWeakTable<Delegate, Delegate[]>();
        private static readonly ConditionalWeakTable<Delegate, Delegate[]>.CreateValueCallback ReadInvocationList = action => action.GetInvocationList();

        public static void OnLoad()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("BSEvents");
            go.AddComponent<BSEvents>();
        }

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

#if !LEGATO_BSUTILS_EARLY
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
#endif

            DontDestroyOnLoad(gameObject);
        }

#if !LEGATO_BSUTILS_EARLY
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
#if !LEGATO_BEFORE_MULTIPLAYER
            if (arg1.name != SceneNames.Game) ClearMultiplayerStateHandler();
#endif
            //    Utilities.Logger.log.Info(arg1.name);
            try
            {
                if (arg1.name == SceneNames.Game)
                {

                    InvokeSafely(gameSceneActive);

                    gameScenesManager = Legato.Unity.ObjectDiscovery.FindLoaded<GameScenesManager>().FirstOrDefault();

                    if (gameScenesManager != null)
                    {
                        gameScenesManager.transitionDidFinishEvent -= GameSceneLoadedCallback;
                        gameScenesManager.transitionDidFinishEvent += GameSceneLoadedCallback;
                    }
                }
                else if (arg1.name == SceneNames.Menu)
                {
                    gameScenesManager = Legato.Unity.ObjectDiscovery.FindLoaded<GameScenesManager>().FirstOrDefault();

                    InvokeSafely(menuSceneActive);

                    if (gameScenesManager != null)
                    {

                        if (arg0.name == SceneNames.EmptyTransition && !lastMainSceneWasNotMenu)
                        {
                            //     Utilities.Logger.log.Info("Fresh");

                            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoadedFresh;
                            gameScenesManager.transitionDidFinishEvent += OnMenuSceneWasLoadedFresh;
                        }
                        else
                        {
                            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoaded;
                            gameScenesManager.transitionDidFinishEvent += OnMenuSceneWasLoaded;
                        }
                    }
                    lastMainSceneWasNotMenu = false;
                }
                if (MainSceneNames.Contains(arg1.name))
                    lastMainSceneWasNotMenu = true;
            }
            catch (Exception e)
            {
                Logger.log.Error(e);
            }
        }

        private void OnMenuSceneWasLoaded(
#if !LEGATO_UNTYPED_SCENE_TRANSITIONS
            SceneTransitionType sceneTransitionType,
#endif
            ScenesTransitionSetupData transitionSetupData, DiContainer diContainer)
        {
            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoaded;
            InvokeSafely(menuSceneLoaded);
        }

        private void OnMenuSceneWasLoadedFresh(
#if !LEGATO_UNTYPED_SCENE_TRANSITIONS
            SceneTransitionType sceneTransitionType,
#endif
            ScenesTransitionSetupData transitionSetupData, DiContainer diContainer)
        {
            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoadedFresh;

            var levelDetailViewController = Legato.Unity.ObjectDiscovery.FindLoaded<StandardLevelDetailViewController>().FirstOrDefault();
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            levelDetailViewController.didChangeDifficultyBeatmapEvent += delegate (StandardLevelDetailViewController vc, IDifficultyBeatmap beatmap) { InvokeSafely(difficultySelected, vc); };
#else
            levelDetailViewController.didChangeDifficultyBeatmapEvent += delegate (StandardLevelDetailViewController vc) { InvokeSafely(difficultySelected, vc); };
#endif

            var characteristicSelect = Legato.Unity.ObjectDiscovery.FindLoaded<BeatmapCharacteristicSegmentedControlController>().FirstOrDefault();
#if LEGATO_SCRIPTABLE_CHARACTERISTICS
            characteristicSelect.didSelectBeatmapCharacteristicEvent += delegate (BeatmapCharacteristicSegmentedControlController controller, BeatmapCharacteristicSO characteristic)
            {
                InvokeSafely(characteristicSelected, controller, characteristic);
            };
#else
            characteristicSelect.didSelectBeatmapCharacteristicEvent += delegate (BeatmapCharacteristicSegmentedControlController controller, BeatmapCharacteristic characteristic)
            {
                if (characteristicSelected != null)
                    InvokeSafely(characteristicSelected, controller, controller._beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(characteristic.SerializedName()));
            };

#endif

            var packSelectViewController = Legato.Unity.ObjectDiscovery.FindLoaded<LevelSelectionNavigationController>().FirstOrDefault();
            packSelectViewController.didSelectLevelPackEvent += delegate (LevelSelectionNavigationController controller, NativeBeatmapLevelPack pack) { InvokeSafely(levelPackSelected, controller, pack); };
            var levelSelectViewController = Legato.Unity.ObjectDiscovery.FindLoaded<LevelCollectionViewController>().FirstOrDefault();
            levelSelectViewController.didSelectLevelEvent += delegate (LevelCollectionViewController controller, BeatmapLevel level) { InvokeSafely(levelSelected, controller, level); };

            InvokeSafely(earlyMenuSceneLoadedFresh, transitionSetupData);
            InvokeSafely(menuSceneLoadedFresh);
            InvokeSafely(lateMenuSceneLoadedFresh, transitionSetupData);
        }

        private void GameSceneLoadedCallback(
#if !LEGATO_UNTYPED_SCENE_TRANSITIONS
            SceneTransitionType sceneTransitionType,
#endif
            ScenesTransitionSetupData transitionSetupData, DiContainer diContainer)
        {
#if !LEGATO_BEFORE_MULTIPLAYER
            ClearMultiplayerStateHandler();
#endif
            // Prevent firing this event when returning to menu
            gameScenesManager.transitionDidFinishEvent -= GameSceneLoadedCallback;
#if !LEGATO_BEFORE_MULTIPLAYER
            if (Plugin.LevelData.Mode == Gameplay.Mode.Multiplayer)
            {
                MultiplayerController sync = Legato.Unity.ObjectDiscovery.FindLoaded<MultiplayerController>().LastOrDefault(x => x.isActiveAndEnabled);
                if (sync != null)
                {
                    _multiplayerController = sync;
                    _multiplayerStateHandler = state =>
                    {
                        if (state != MultiplayerController.State.Gameplay) return;
                        ClearMultiplayerStateHandler();
                        GameSceneSceneWasLoaded(transitionSetupData, diContainer);
                    };
                    sync.stateChangedEvent += _multiplayerStateHandler;
                }
                return;
            }
#endif
            GameSceneSceneWasLoaded(transitionSetupData, diContainer);
        }

#if !LEGATO_BEFORE_MULTIPLAYER
        private void ClearMultiplayerStateHandler()
        {
            if (_multiplayerController != null && _multiplayerStateHandler != null)
                _multiplayerController.stateChangedEvent -= _multiplayerStateHandler;
            _multiplayerController = null;
            _multiplayerStateHandler = null;
        }
#endif

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
#if !LEGATO_BEFORE_MULTIPLAYER
            ClearMultiplayerStateHandler();
#endif
            if (gameScenesManager != null)
            {
                gameScenesManager.transitionDidFinishEvent -= GameSceneLoadedCallback;
                gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoaded;
                gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoadedFresh;
            }
            Instance = null;
        }

        private void GameSceneSceneWasLoaded(ScenesTransitionSetupData transitionSetupData, DiContainer diContainer)
        {
            //Debug.Log("GameSceneSceneWasLoaded()");

            var pauseManager = diContainer.TryResolve<PauseController>();
            if (pauseManager != null)
            {
                pauseManager.didResumeEvent += delegate { InvokeSafely(songUnpaused); };
                pauseManager.didPauseEvent += delegate { InvokeSafely(songPaused); };
            }

            var beatmapObjectManager = diContainer.TryResolve<BeatmapObjectManager>();
            if (beatmapObjectManager != null)
            {
#if LEGATO_BEFORE_MULTIPLAYER
                beatmapObjectManager.noteWasCutEvent += (controller, noteCutInfo) =>
                {
                    if (controller is NoteController note) InvokeSafely(noteWasCut, note, noteCutInfo);
                };
                beatmapObjectManager.noteWasMissedEvent += controller =>
                {
                    if (controller is NoteController note) InvokeSafely(noteWasMissed, note);
                };
#else
                beatmapObjectManager.noteWasCutEvent += (NoteController controller, in NoteCutInfo noteCutInfo) => InvokeSafely(noteWasCut, controller, noteCutInfo);
                beatmapObjectManager.noteWasMissedEvent += controller => InvokeSafely(noteWasMissed, controller);
#endif
            }

#if !LEGATO_BEFORE_MULTIPLAYER
            var comboController = diContainer.TryResolve<ComboController>();
            if (comboController != null)
            {
                comboController.comboDidChangeEvent += delegate (int combo) { InvokeSafely(comboDidChange, combo); };
                comboController.comboBreakingEventHappenedEvent += delegate { InvokeSafely(comboDidBreak); };
            }
#endif

            var scoreController = diContainer.TryResolve<ScoreController>();
            if (scoreController != null)
            {
#if LEGATO_BEFORE_MULTIPLAYER
                scoreController.comboDidChangeEvent += combo => InvokeSafely(comboDidChange, combo);
                scoreController.comboBreakingEventHappenedEvent += () => InvokeSafely(comboDidBreak);
#endif
                scoreController.multiplierDidChangeEvent += delegate (int multiplier, float progress) { InvokeSafely(multiplierDidChange, multiplier, progress);
                if (multiplier > 1 && progress < 0.1f)
                    InvokeSafely(multiplierDidIncrease, multiplier); };
                scoreController.scoreDidChangeEvent += (score, modifiedScore) => InvokeSafely(scoreDidChange, score);
            }

            var saberCollisionManager = Legato.Unity.ObjectDiscovery.FindLoaded<ObstacleSaberSparkleEffectManager>().LastOrDefault(x => x.isActiveAndEnabled);
            if (saberCollisionManager != null)
            {
                saberCollisionManager.sparkleEffectDidStartEvent += delegate (SaberType saber) { InvokeSafely(sabersStartCollide, saber); };
                saberCollisionManager.sparkleEffectDidEndEvent += delegate (SaberType saber) { InvokeSafely(sabersEndCollide, saber); };
            }

            var gameEnergyCounter = Legato.Unity.ObjectDiscovery.FindLoaded<GameEnergyCounter>().LastOrDefault(x => x.isActiveAndEnabled);
            if (gameEnergyCounter != null)
            {
                gameEnergyCounter.gameEnergyDidReach0Event += delegate { InvokeSafely(energyReachedZero); };
                gameEnergyCounter.gameEnergyDidChangeEvent += delegate (float energy) { InvokeSafely(energyDidChange, energy); };
            }

#if LEGATO_BEFORE_MULTIPLAYER
            var beatmapCallbacksController = diContainer.TryResolve<BeatmapObjectCallbackController>();
            if (beatmapCallbacksController != null)
            {
                beatmapCallbacksController.beatmapEventDidTriggerEvent += songEvent => InvokeSafely(beatmapEvent, songEvent);
            }
            var transitionSetup = transitionSetupData as StandardLevelScenesTransitionSetupData;
#else
            var beatmapCallbacksController = diContainer.TryResolve<BeatmapCallbacksController>();
            beatmapCallbacksController?.AddBeatmapCallback(new BeatmapDataCallback<BeatmapEventData>(songEvent => InvokeSafely(beatmapEvent, songEvent)));

            var transitionSetup = diContainer.TryResolve<StandardLevelScenesTransitionSetupData>();
#endif
            if (transitionSetup != null)
            {
                transitionSetup.didFinishEvent -= OnTransitionSetupOnDidFinishEvent;
                transitionSetup.didFinishEvent += OnTransitionSetupOnDidFinishEvent;
            }

            InvokeSafely(gameSceneLoaded);
        }

        private void OnTransitionSetupOnDidFinishEvent(StandardLevelScenesTransitionSetupData data, LevelCompletionResults results)
        {
            switch (results.levelEndStateType)
            {
                case LevelCompletionResults.LevelEndStateType.Cleared:
                    InvokeSafely(levelCleared, data, results);
                    break;
                case LevelCompletionResults.LevelEndStateType.Failed:
                    if (results.levelEndAction != LevelCompletionResults.LevelEndAction.Restart)
                        InvokeSafely(levelFailed, data, results);
                    break;
            }

            switch (results.levelEndAction)
            {
                case LevelCompletionResults.LevelEndAction.Quit:
                    InvokeSafely(levelQuit, data, results);
                    break;
                case LevelCompletionResults.LevelEndAction.Restart:
                    InvokeSafely(levelRestarted, data, results);
                    break;
            }
        }

#else
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
#endif

        private static void InvokeSafely(Action action)
        {
            if (action == null) return;
            foreach (Action handler in InvocationLists.GetValue(action, ReadInvocationList))
            {
                try { handler(); }
                catch (Exception ex) { LogHandlerException(handler, ex); }
            }
        }

        private static void InvokeSafely<T>(Action<T> action, T arg)
        {
            if (action == null) return;
            foreach (Action<T> handler in InvocationLists.GetValue(action, ReadInvocationList))
            {
                try { handler(arg); }
                catch (Exception ex) { LogHandlerException(handler, ex); }
            }
        }

        private static void InvokeSafely<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action == null) return;
            foreach (Action<T1, T2> handler in InvocationLists.GetValue(action, ReadInvocationList))
            {
                try { handler(arg1, arg2); }
                catch (Exception ex) { LogHandlerException(handler, ex); }
            }
        }

        private static void LogHandlerException(Delegate handler, Exception ex)
        {
            Logger.log.Error($"Caught Exception when executing event: {ex.Message}\n In Assembly: {handler.Method.DeclaringType?.Assembly.FullName}");
            Logger.log.Debug(ex);
        }

        // Retain the public params API for existing callers. Internal events use typed dispatch.
        public void InvokeAll<T1, T2, T3>(Action<T1, T2, T3> action, params object[] args)
        {
            Delegate[] actions = action?.GetInvocationList();
            if (actions == null) return;
            foreach (Delegate invoc in actions)
            {
                string name = "";
                try
                {
                    name = invoc?.Method.DeclaringType.Assembly.FullName;
                    invoc?.DynamicInvoke(args);
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Caught Exception when executing event: {e.Message}\n In Assembly: {name}");
                    Logger.log.Debug(e);
                }
            }
        }
        public void InvokeAll<T1, T2>(Action<T1, T2> action, params object[] args)
        {
            Delegate[] actions = action?.GetInvocationList();
            if (actions == null) return;
            foreach (Delegate invoc in actions)
            {
                string name = "";
                try
                {
                    name = invoc?.Method.DeclaringType.Assembly.FullName;
                    invoc?.DynamicInvoke(args);
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Caught Exception when executing event: {e.Message}\n In Assembly: {name}");
                    Logger.log.Debug(e);
                }
            }
        }

        public void InvokeAll<T>(Action<T> action, params object[] args)
        {
            Delegate[] actions = action?.GetInvocationList();
            if (actions == null) return;
            foreach (Delegate invoc in actions)
            {
                string name = "";
                try
                {
                    name = invoc?.Method.DeclaringType.Assembly.FullName;
                    invoc?.DynamicInvoke(args);
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Caught Exception when executing event: {e.Message}\n In Assembly: {name}");
                    Logger.log.Debug(e);
                }
            }
        }
        public void InvokeAll(Action action, params object[] args)
        {
            Delegate[] actions = action?.GetInvocationList();
            if (actions == null) return;
            foreach (Delegate invoc in actions)
            {
                string name = "";
                try
                {
                    name = invoc?.Method.DeclaringType.Assembly.FullName;
                    invoc?.DynamicInvoke(args);
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Caught Exception when executing event: {e.Message}\n In Assembly: {name}");
                    Logger.log.Debug(e);
                }
            }
        }

        #region LevelFinishedInvokers
        internal static void TriggerLevelFinishEvent(StandardLevelScenesTransitionSetupData levelScenesTransitionSetupData, LevelCompletionResults levelCompletionResults)
        {
            Logger.log.Debug("Solo/Party mode level finished.");
            LevelFinished?.RaiseEventSafe(levelScenesTransitionSetupData,
                new SoloLevelFinishedEventArgs(levelScenesTransitionSetupData, levelCompletionResults),
                nameof(LevelFinished));
        }

#if !LEGATO_BEFORE_MULTIPLAYER
        internal static void TriggerMultiplayerLevelDidFinish(MultiplayerLevelScenesTransitionSetupData levelScenesTransitionSetupData, LevelCompletionResults levelCompletionResults, IReadOnlyList<MultiplayerPlayerResultsData> otherPlayersLevelCompletionResults)
        {
            Logger.log.Debug("Multiplayer level finished.");
            LevelFinished?.RaiseEventSafe(levelScenesTransitionSetupData,
                new MultiplayerLevelFinishedEventArgs(levelScenesTransitionSetupData, levelCompletionResults, otherPlayersLevelCompletionResults),
                nameof(LevelFinished));
        }
#endif
#if !LEGATO_BSUTILS_EARLY
        internal static void TriggerMissionFinishEvent(MissionLevelScenesTransitionSetupData missionLevelScenesTransitionSetupData, MissionCompletionResults missionCompletionResults)
        {
            Logger.log.Debug("Campaign level finished.");
            LevelFinished?.RaiseEventSafe(missionLevelScenesTransitionSetupData,
                new CampaignLevelFinishedEventArgs(missionLevelScenesTransitionSetupData, missionCompletionResults),
                nameof(LevelFinished));
        }
#endif

        internal static void TriggerTutorialFinishEvent(TutorialScenesTransitionSetupData tutorialLevelScenesTransitionSetupData,
#if LEGATO_BSUTILS_EARLY
            bool endState)
#else
            TutorialScenesTransitionSetupData.TutorialEndStateType endState)
#endif
        {
            Logger.log.Debug("Tutorial level finished.");
            LevelFinished?.RaiseEventSafe(tutorialLevelScenesTransitionSetupData,
                new TutorialLevelFinishedEventArgs(tutorialLevelScenesTransitionSetupData, endState),
                nameof(LevelFinished));
        }
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static void TriggerMultiplayerDidDisconnect(MultiplayerLevelScenesTransitionSetupData levelScenesTransitionSetupData, DisconnectedReason disconnectedReason)
        {
            Logger.log.Debug("Multiplayer did disconnect.");
            MultiplayerDidDisconnect?.RaiseEventSafe(levelScenesTransitionSetupData,
                (levelScenesTransitionSetupData, disconnectedReason),
                nameof(MultiplayerDidDisconnect));
        }
#endif
        #endregion
    }
}
