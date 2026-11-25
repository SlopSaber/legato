using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Heck.HarmonyPatches;
using HMUI;
using JetBrains.Annotations;
using ModestTree;
using Zenject;

namespace Heck.PlayView;

public sealed class PlayViewManager : IDisposable
{
    private readonly bool[] _doPresent;
#if !LEGATO_HECK_LEGACY_GAMEPLAY
    private readonly GameServerLobbyFlowCoordinator _gameServerLobbyFlowCoordinator;
    private readonly LobbyGameStateController _lobbyGameStateController;
#endif
    private readonly MenuTransitionsHelper _menuTransitionsHelper;
    private readonly PartyFreePlayFlowCoordinator _partyFreePlayFlowCoordinator;
    private readonly SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator;
    private readonly PlayViewControllerData[] _viewControllers;

    private int _currentIndex;
    private StartStandardLevelParameters? _currentParameters;

    private FlowCoordinator _flowCoordinator;
#if LEGATO_HECK_LEGACY_GAMEPLAY
    private bool _viewTransitionInProgress;
#else
    private bool _modifiedParameters;
#endif

    private Queue<Action> _transitionFinished = new();

    [UsedImplicitly]
    private PlayViewManager(
        [Inject(Optional = true, Source = InjectSources.Local)]
        IEnumerable<IPlayViewController> viewControllers,
        SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator,
        PartyFreePlayFlowCoordinator partyFreePlayFlowCoordinator,
#if !LEGATO_HECK_LEGACY_GAMEPLAY
        GameServerLobbyFlowCoordinator gameServerLobbyFlowCoordinator,
        FlowCoordinatorTransitionListener flowCoordinatorTransitionListener,
        ILobbyGameStateController lobbyGameStateController,
#endif
        MenuTransitionsHelper menuTransitionsHelper)
    {
        _viewControllers =
            viewControllers.Select(n => new PlayViewControllerData(n)).OrderBy(n => n.Priority).ToArray();
        _doPresent = new bool[_viewControllers.Length];
        foreach (PlayViewControllerData playViewControllerData in _viewControllers)
        {
            IPlayViewController viewController = playViewControllerData.ViewController;
            viewController.Finished += Activate;
            if (viewController is IParameterModifier parameterModifier)
            {
                parameterModifier.ParametersModified += HandleParametersModified;
            }
        }

        _flowCoordinator = soloFreePlayFlowCoordinator;
        _soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
        _partyFreePlayFlowCoordinator = partyFreePlayFlowCoordinator;
#if !LEGATO_HECK_LEGACY_GAMEPLAY
        _gameServerLobbyFlowCoordinator = gameServerLobbyFlowCoordinator;
        flowCoordinatorTransitionListener.TransitionFinished += OnTransitionFinished;
        _lobbyGameStateController = (LobbyGameStateController)lobbyGameStateController;
#endif
        _menuTransitionsHelper = menuTransitionsHelper;
    }

    private event Action TransitionFinished
    {
        add
        {
            if (!ViewTransitionInProgress)
            {
                value();
                return;
            }

            _transitionFinished.Enqueue(value);
        }

        remove => _transitionFinished = new Queue<Action>(_transitionFinished.Where(n => n != value));
    }

    internal PlayViewControllerData? ActiveView { get; private set; }

#if LEGATO_HECK_LEGACY_GAMEPLAY
    private bool ViewTransitionInProgress => _viewTransitionInProgress;
#else
    private bool ViewTransitionInProgress => _flowCoordinator._isInTransition;
#endif

    public void Dispose()
    {
        foreach (PlayViewControllerData playViewControllerData in _viewControllers)
        {
            IPlayViewController viewController = playViewControllerData.ViewController;
            viewController.Finished -= Activate;
            if (viewController is IParameterModifier parameterModifier)
            {
                parameterModifier.ParametersModified -= HandleParametersModified;
            }
        }
    }

    // SEND IT BABY!!!!
    [PublicAPI]
    public void ForceStart(StartStandardLevelParameters startParameters)
    {
#if LEGATO_HECK_LEGACY_GAMEPLAY
        Init(startParameters, _soloFreePlayFlowCoordinator.isActivated ? _soloFreePlayFlowCoordinator : _partyFreePlayFlowCoordinator, true);
#else
        Init(startParameters, true);
#endif
        StartStandard();
    }

    internal bool EarlyDismiss()
    {
        if (ActiveView == null)
        {
            return true;
        }

        ActiveView.InvokeOnEarlyDismiss();
#if LEGATO_HECK_LEGACY_GAMEPLAY
        var view = (ViewController)ActiveView.ViewController;
        ActiveView = null;
        _viewTransitionInProgress = true;
        _flowCoordinator.DismissViewController(view, FinishViewTransition, false);
#else
        _flowCoordinator.DismissViewController((ViewController)ActiveView.ViewController);
        ActiveView = null;
#endif
        return false;
    }

    internal void FinishAll()
    {
        foreach (PlayViewControllerData playViewControllerData in _viewControllers)
        {
            playViewControllerData.InvokeOnFinish();
        }
    }

#if LEGATO_HECK_LEGACY_GAMEPLAY
    internal void Init(StartStandardLevelParameters startParameters, FlowCoordinator flowCoordinator, bool forced = false)
#else
    internal void Init(StartStandardLevelParameters startParameters, bool forced = false)
#endif
    {
        _currentIndex = 0;
        _currentParameters = startParameters;
#if LEGATO_HECK_LEGACY_GAMEPLAY
        _flowCoordinator = flowCoordinator;
#else
        _modifiedParameters = false;
#endif
        for (int i = 0; i < _viewControllers.Length; i++)
        {
            _doPresent[i] = _viewControllers[i].ViewController.Init(_currentParameters);
        }

        if (forced)
        {
            return;
        }

#if !LEGATO_HECK_LEGACY_GAMEPLAY
        if (startParameters is StartMultiplayerLevelParameters)
        {
            _flowCoordinator = _gameServerLobbyFlowCoordinator;
        }
        else
        {
            _flowCoordinator = _soloFreePlayFlowCoordinator.isActivated
                ? _soloFreePlayFlowCoordinator
                : _partyFreePlayFlowCoordinator;
        }
#endif

        Activate();
    }

#if !LEGATO_HECK_LEGACY_GAMEPLAY
    internal bool StartMultiplayer()
    {
        TransitionFinished += Dismiss;

        foreach (PlayViewControllerData playViewControllerData in _viewControllers)
        {
            playViewControllerData.InvokeOnPlay();
        }

        if (!_modifiedParameters ||
            _currentParameters is not StartMultiplayerLevelParameters multiplayerStartParameters)
        {
            return true;
        }

        _lobbyGameStateController.countdownStarted = false;
        _lobbyGameStateController.StopListeningToGameStart();

#if BEAT_SABER_1453
        GameMode gameMode = (GameMode)Enum.Parse(typeof(GameMode), multiplayerStartParameters.GameMode);
#else
        string gameMode = multiplayerStartParameters.GameMode;
#endif
        _menuTransitionsHelper.StartMultiplayerLevel(
            gameMode,
#if !PRE_V1_37_1
            multiplayerStartParameters.BeatmapKey,
            multiplayerStartParameters.BeatmapLevel,
            multiplayerStartParameters.BeatmapLevelData!,
#else
            multiplayerStartParameters.PreviewBeatmapLevel,
            multiplayerStartParameters.BeatmapDifficulty,
            multiplayerStartParameters.BeatmapCharacteristic,
            multiplayerStartParameters.DifficultyBeatmap,
#endif
            multiplayerStartParameters.OverrideColorScheme!,
            multiplayerStartParameters.GameplayModifiers,
            multiplayerStartParameters.PlayerSpecificSettings,
#if LATEST
            multiplayerStartParameters.EnvironmentsListModel!,
#endif
            multiplayerStartParameters.PracticeSettings!,
#if LATEST
            string.Empty,
            false,
#else
            multiplayerStartParameters.BackButtonText,
            multiplayerStartParameters.UseTestNoteCutSoundEffects,
#endif
            multiplayerStartParameters.BeforeSceneSwitchCallback!,
            multiplayerStartParameters.MultiplayerLevelFinishedCallback!,
            multiplayerStartParameters.DidDisconnectCallback!);
        return false;
    }
#endif

    private void Activate()
    {
        if (_currentParameters == null)
        {
            throw new InvalidOperationException();
        }

        TransitionFinished += () =>
        {
            while (_currentIndex < _viewControllers.Length)
            {
                int index = _currentIndex++;
                PlayViewControllerData viewControllerData = _viewControllers[index];
                IPlayViewController viewController = viewControllerData.ViewController;
                if (!_doPresent[index])
                {
                    continue;
                }

                bool newView = ActiveView == null;
                ActiveView = viewControllerData;

#if LEGATO_HECK_LEGACY_GAMEPLAY
                _viewTransitionInProgress = true;
                if (newView)
                {
                    _flowCoordinator.PresentViewController((ViewController)viewController, FinishViewTransition, false);
                }
                else
                {
                    _flowCoordinator.ReplaceTopViewController((ViewController)viewController, FinishViewTransition, false, default);
                }

                _flowCoordinator.PresentTitle(viewControllerData.Title, false);
#else
                if (newView)
                {
                    _flowCoordinator.PresentViewController((ViewController)viewController);
                }
                else
                {
                    _flowCoordinator.ReplaceTopViewController((ViewController)viewController);
                }

                _flowCoordinator.SetTitle(viewControllerData.Title);
#endif

                viewControllerData.InvokeOnShow();

                return;
            }

            Dismiss();

#if LEGATO_HECK_LEGACY_GAMEPLAY
            StartStandard();
#else
            if (_currentParameters is not StartMultiplayerLevelParameters)
            {
                StartStandard();
            }
#endif
        };
    }

    private void Dismiss()
    {
        if (ActiveView == null)
        {
            return;
        }

#if LEGATO_HECK_LEGACY_GAMEPLAY
        _flowCoordinator.DismissViewController((ViewController)ActiveView.ViewController, null, true);
#else
        _flowCoordinator.DismissViewController(
            (ViewController)ActiveView.ViewController,
            ViewController.AnimationDirection.Horizontal,
            null,
            true);
#endif
        ActiveView = null;
    }

    private void HandleParametersModified(StartStandardLevelParameters startParameters)
    {
#if !LEGATO_HECK_LEGACY_GAMEPLAY
        _modifiedParameters = true;
#endif
        _currentParameters = startParameters;
    }

    private void StartStandard()
    {
        if (_currentParameters == null)
        {
            return;
        }

        foreach (PlayViewControllerData playViewControllerData in _viewControllers)
        {
            playViewControllerData.InvokeOnPlay();
        }

#if LEGATO_HECK_LEGACY_GAMEPLAY
        _menuTransitionsHelper.StartStandardLevel(
            _currentParameters.DifficultyBeatmap,
            _currentParameters.OverrideEnvironmentSettings,
            _currentParameters.OverrideColorScheme,
            _currentParameters.GameplayModifiers,
            _currentParameters.PlayerSpecificSettings,
            _currentParameters.PracticeSettings,
            _currentParameters.BackButtonText,
            _currentParameters.UseTestNoteCutSoundEffects,
            _currentParameters.BeforeSceneSwitchCallback,
            _currentParameters.LevelFinishedCallback);
#else
#if BEAT_SABER_1453
        GameMode gameMode = (GameMode)Enum.Parse(typeof(GameMode), _currentParameters.GameMode);
#else
        string gameMode = _currentParameters.GameMode;
#endif
        _menuTransitionsHelper.StartStandardLevel(
            gameMode,
#if !PRE_V1_37_1
            _currentParameters.BeatmapKey,
            _currentParameters.BeatmapLevel,
#else
            _currentParameters.DifficultyBeatmap,
            _currentParameters.PreviewBeatmapLevel,
#endif
            _currentParameters.OverrideEnvironmentSettings,
            _currentParameters.OverrideColorScheme,
#if !PRE_V1_40_8
            _currentParameters.PlayerOverrideLightshowColors,
#endif
#if !V1_29_1 && !LATEST
            _currentParameters.BeatmapOverrideColorScheme,
#endif
            _currentParameters.GameplayModifiers,
            _currentParameters.PlayerSpecificSettings,
            _currentParameters.PracticeSettings,
#if !PRE_V1_37_1
            _currentParameters.EnvironmentsListModel!,
#endif
#if LATEST
            _currentParameters.GameplayAdditionalInformation!,
#else
            _currentParameters.BackButtonText,
            _currentParameters.UseTestNoteCutSoundEffects,
            _currentParameters.StartPaused,
#endif
            _currentParameters.BeforeSceneSwitchCallback,
#if !PRE_V1_37_1
            _currentParameters.AfterSceneSwitchCallback,
#else
            null,
#endif
            _currentParameters.LevelFinishedCallback,
#if LATEST
            _currentParameters.LevelRestartedCallback,
#if LEGATO_SCRIPTABLE_SCENE_TRANSITIONS
            _currentParameters.BeatmapLevelData,
            _currentParameters.RecordingToolData);
#else
            _currentParameters.BeatmapLevelData);
#endif
#elif !V1_29_1
            _currentParameters.LevelRestartedCallback,
            _currentParameters.RecordingToolData);
#else
            _currentParameters.LevelRestartedCallback);
#endif
#endif
    }

#if LEGATO_HECK_LEGACY_GAMEPLAY
    private void FinishViewTransition()
    {
        _viewTransitionInProgress = false;
        OnTransitionFinished(_flowCoordinator);
    }

#endif
    private void OnTransitionFinished(FlowCoordinator flowCoordinator)
    {
        if (flowCoordinator != _flowCoordinator)
        {
            return;
        }

        while (!ViewTransitionInProgress && _transitionFinished.Count > 0)
        {
            _transitionFinished.Dequeue().Invoke();
        }
    }

    internal class PlayViewControllerData
    {
        private readonly MethodInfo? _onEarlyDismiss;

        private readonly MethodInfo? _onFinish;
        private readonly MethodInfo? _onPlay;

        private readonly MethodInfo? _onShow;

        internal PlayViewControllerData(IPlayViewController viewController)
        {
            ViewController = viewController;
            Type type = viewController.GetType();
            PlayViewControllerSettingsAttribute? settingsAttribute =
                type.TryGetAttribute<PlayViewControllerSettingsAttribute>();
            Priority = settingsAttribute?.Priority ?? 0;
            Title = settingsAttribute?.Title ?? string.Empty;
            _onPlay = type.GetMethod("OnPlay", AccessTools.all);
            _onFinish = type.GetMethod("OnFinish", AccessTools.all);
            _onEarlyDismiss = type.GetMethod("OnEarlyDismiss", AccessTools.all);
            _onShow = type.GetMethod("OnShow", AccessTools.all);
        }

        internal int Priority { get; }

        internal string Title { get; }

        internal IPlayViewController ViewController { get; }

        internal void InvokeOnEarlyDismiss()
        {
            _onEarlyDismiss?.Invoke(ViewController, []);
        }

        internal void InvokeOnFinish()
        {
            _onFinish?.Invoke(ViewController, []);
        }

        internal void InvokeOnPlay()
        {
            _onPlay?.Invoke(ViewController, []);
        }

        internal void InvokeOnShow()
        {
            _onShow?.Invoke(ViewController, []);
        }
    }
}
