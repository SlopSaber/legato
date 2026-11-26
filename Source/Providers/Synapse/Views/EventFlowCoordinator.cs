using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HMUI;
using IPA.Utilities;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SiraUtil.Logging;
using Synapse.Extras;
using Synapse.HarmonyPatches;
using Synapse.Managers;
using Synapse.Models;
using Synapse.Networking.Models;
using UnityEngine;
using Zenject;
#if !LEGATO_DIFFICULTY_BEATMAPS
using System.Reflection;
using HarmonyLib;
#endif

namespace Synapse.Views;

internal class EventFlowCoordinator : FlowCoordinator
{
    private const int SCORE_SUBMISSION_INTERVAL = 2000;
    private const int SCORE_SUBMISSION_ATTEMPTS = 4;

    private SiraLog _log = null!;
    private Config _config = null!;
    private GameplaySetupViewController _gameplaySetupViewController = null!;
    private ResultsViewController _resultsViewController = null!;
    private EventDivisionSelectViewController _divisionSelectViewController = null!;
    private EventIntroViewController _introViewController = null!;
    private EventLeaderboardViewController _leaderboardViewController = null!;
    private EventLoadingViewController _loadingViewController = null!;
    private EventLobbyNavigationViewController _lobbyNavigationViewController = null!;
    private EventModsDownloadingViewController _modsDownloadingViewController = null!;
    private EventModsViewController _modsViewController = null!;
    private SimpleDialogPromptViewController _simpleDialogPromptViewController = null!;
    private CountdownManager _countdownManager = null!;
    private MapDownloadingManager _mapDownloadingManager = null!;
    private LevelStartManager _levelStartManager = null!;
    private NetworkManager _networkManager = null!;
    private MenuPrefabManager _menuPrefabManager = null!;
    private GlobalParticleManager _globalParticleManager = null!;

#if !LEGATO_DIFFICULTY_BEATMAPS
    private MethodInfo _resultsViewControllerInit = null!;
#endif

    private bool _dirtyListing;
    private Listing? _listing;

    private Queue<Action> _transitionFinished = new();
#if LEGATO_BEFORE_MULTIPLAYER
    private bool _presentingEventView;
    private bool _startingEventLevel;
#endif

    internal event Action<EventFlowCoordinator>? Finished;

    private event Action TransitionFinished
    {
        add
        {
#if LEGATO_BEFORE_MULTIPLAYER
            UnityMainThreadTaskScheduler.Factory.StartNew(
                () =>
                {
                    if (isActivated)
                    {
                        _transitionFinished.Enqueue(value);
                        FinishQueuedTransitions();
                    }
                });
#else
            if (!TransitionBusy)
            {
                UnityMainThreadTaskScheduler.Factory.StartNew(value);
                return;
            }

            _transitionFinished.Enqueue(value);
#endif
        }

        remove => _transitionFinished = new Queue<Action>(_transitionFinished.Where(n => n != value));
    }

    // screw it
    public static bool IsActive { get; private set; }

#if LEGATO_BEFORE_MULTIPLAYER
    private bool TransitionBusy => _presentingEventView || _startingEventLevel ||
                                   topViewController is { _isInTransition: true };
#else
    private bool TransitionBusy => _isInTransition;
#endif

    public override void BackButtonWasPressed(ViewController topView)
    {
#if LEGATO_BEFORE_MULTIPLAYER
        if (!TransitionBusy)
#else
        if (!topView.isInTransition)
#endif
        {
            Finished?.Invoke(this);
        }
    }

#if LEGATO_BEFORE_MULTIPLAYER
    public override void DidActivate(bool firstActivation, ActivationType activationType)
    {
        bool addedToHierarchy = activationType == ActivationType.AddedToHierarchy;
#else
    public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
#endif

        // TODO: light color presets
        // ReSharper disable once InvertIf
        if (addedToHierarchy)
        {
            IsActive = true;
            SetEventTitle(_listing?.Title ?? "N/A");
            showBackButton = true;

#if LEGATO_BEFORE_MULTIPLAYER
            _gameplaySetupViewController.Setup(false, false, true);
#else
            _gameplaySetupViewController.Setup(
                false,
                false,
                true,
                false,
                PlayerSettingsPanelController.PlayerSettingsPanelLayout.Singleplayer);
#endif

            if (_listing == null)
            {
                SetEventTitle(null);
                showBackButton = false;
                _simpleDialogPromptViewController.Init(
                    "Error",
                    "Listing failed to load",
                    "Ok",
                    _ => { Finished?.Invoke(this); });
                ProvideInitialViewControllers(_simpleDialogPromptViewController);
            }
            else if (_modsDownloadingViewController.DownloadFinished)
            {
                ProvideInitialViewControllers(_modsDownloadingViewController);
            }
            else
            {
                if (_dirtyListing)
                {
                    if (!_listing.GameVersion.MatchesGameVersion())
                    {
                        SetEventTitle(null);
                        showBackButton = false;
                        _simpleDialogPromptViewController.Init(
                            "Error",
                            $"{_listing.Title} only allows versions {_listing.GameVersion.Replace(",", ", ")}",
                            "Ok",
                            _ => { Finished?.Invoke(this); });
                        ProvideInitialViewControllers(_simpleDialogPromptViewController);
                        return;
                    }

                    List<ModInfo>? modsToDownload = _modsViewController.Init();
                    if (modsToDownload != null)
                    {
                        _modsDownloadingViewController.Init(modsToDownload);
                        _modsViewController.Finished += OnAcceptModsDownload;
                        ProvideInitialViewControllers(_modsViewController);
                        return;
                    }
                }

                _dirtyListing = false;

                if (_listing.Time.ToTimeSpan().Ticks > 0)
                {
                    SetEventTitle(null);
                    showBackButton = false;
                    _simpleDialogPromptViewController.Init(
                        "Error",
                        $"{_listing.Title} has not started yet!",
                        "Ok",
                        _ => { Finished?.Invoke(this); });
                    ProvideInitialViewControllers(_simpleDialogPromptViewController);
                    return;
                }

                _menuPrefabManager.Reset(false);
                _ = _menuPrefabManager.Download();
                _resultsViewController.continueButtonPressedEvent += HandleResultsViewControllerContinueButtonPressed;
                _countdownManager.LevelStarted += OnLevelStarted;
                _lobbyNavigationViewController.IntroStarted += OnIntroStarted;
                _lobbyNavigationViewController.OutroStarted += OnOutroStarted;
                _loadingViewController.Finished += OnLoadingFinished;
                _introViewController.Finished += OnIntroFinished;
                _networkManager.Disconnected += OnDisconnected;
                _menuPrefabManager.Show();
                ShowLoading(true);
            }
        }
    }

#if LEGATO_BEFORE_MULTIPLAYER
    public override void DidDeactivate(DeactivationType deactivationType)
    {
        bool removedFromHierarchy = deactivationType == DeactivationType.RemovedFromHierarchy;
#else
    public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
    {
#endif

        // ReSharper disable once InvertIf
        if (removedFromHierarchy)
        {
#if LEGATO_BEFORE_MULTIPLAYER
            _transitionFinished.Clear();
            _startingEventLevel = false;
            _presentingEventView = false;
#endif
            IsActive = false;
            _resultsViewController.continueButtonPressedEvent -= HandleResultsViewControllerContinueButtonPressed;
            _modsViewController.Finished -= OnAcceptModsDownload;
            _countdownManager.LevelStarted -= OnLevelStarted;
            _lobbyNavigationViewController.IntroStarted -= OnIntroStarted;
            _lobbyNavigationViewController.OutroStarted -= OnOutroStarted;
            _loadingViewController.Finished -= OnLoadingFinished;
            _introViewController.Finished -= OnIntroFinished;
            _networkManager.Disconnected -= OnDisconnected;
            _ = _networkManager.Disconnect(DisconnectCode.DisconnectedByUser);
            _menuPrefabManager.Hide();
        }
    }

    public override void TopViewControllerWillChange(
        ViewController oldViewController,
        ViewController newViewController,
#if LEGATO_BEFORE_MULTIPLAYER
        bool animationType)
#else
        ViewController.AnimationType animationType)
#endif
    {
        switch (newViewController)
        {
            case EventLobbyNavigationViewController:
                SetLeftScreenViewController(_gameplaySetupViewController, animationType);
                SetRightScreenViewController(_leaderboardViewController, animationType);
                break;
            default:
                SetLeftScreenViewController(null, animationType);
                SetRightScreenViewController(null, animationType);
                SetBottomScreenViewController(null, animationType);
                break;
        }

        switch (newViewController)
        {
            case EventLobbyNavigationViewController:
                ApplyTitle(_listing?.Title ?? "N/A");
                showBackButton = true;
                break;
            case EventIntroViewController:
            case SimpleDialogPromptViewController:
                ApplyTitle(null);
                showBackButton = false;
                break;
            case EventLoadingViewController:
            case EventModsDownloadingViewController:
            case EventDivisionSelectViewController:
                ApplyTitle(_listing?.Title ?? "N/A");
                showBackButton = true;
                break;
            default:
                showBackButton = false;
                break;
        }

        void ApplyTitle(string? value)
        {
#if LEGATO_BEFORE_MULTIPLAYER
            _title = value;
            if (!_isInDidActivatePhase && IsFlowCoordinatorInHierarchy(this))
            {
                PresentTitle(value, animationType);
            }
#else
            SetTitle(value, animationType);
#endif
        }
    }

#if LEGATO_BEFORE_MULTIPLAYER
    public override void InitialViewControllerWasPresented()
    {
        base.InitialViewControllerWasPresented();
        FinishQueuedTransitions();
    }

    private void TransitionDidStart()
    {
        _startingEventLevel = true;
    }

    private void TransitionDidFinish()
    {
        _startingEventLevel = false;
        FinishQueuedTransitions();
    }
#else
    public override void TransitionDidFinish()
    {
        base.TransitionDidFinish();
        FinishQueuedTransitions();
    }
#endif

    private void FinishQueuedTransitions()
    {
        while (!TransitionBusy && _transitionFinished.Count > 0)
        {
            _transitionFinished.Dequeue().Invoke();
        }
    }

    private void SetEventTitle(string? value)
    {
#if LEGATO_BEFORE_MULTIPLAYER
        title = value;
#else
        SetTitle(value);
#endif
    }

    private void ReplaceEventView(ViewController view)
    {
#if LEGATO_BEFORE_MULTIPLAYER
        _presentingEventView = true;
        ReplaceTopViewController(view, EventViewPresented, false, ViewController.SlideAnimationDirection.Up);
#else
        ReplaceTopViewController(view, null, ViewController.AnimationType.In, ViewController.AnimationDirection.Vertical);
#endif
    }

    private void PresentEventView(ViewController view, bool horizontal = false, Action? finished = null, bool immediately = false)
    {
#if LEGATO_BEFORE_MULTIPLAYER
        _presentingEventView = true;
        PresentViewController(view, () => EventViewPresented(finished), immediately);
#else
        var direction = horizontal ? ViewController.AnimationDirection.Horizontal : ViewController.AnimationDirection.Vertical;
        PresentViewController(view, finished, direction, immediately);
#endif
    }

    private void DismissEventView(ViewController view, bool horizontal, Action? finished = null)
    {
#if LEGATO_BEFORE_MULTIPLAYER
        _presentingEventView = true;
        DismissViewController(view, () => EventViewPresented(finished), false);
#else
        var direction = horizontal ? ViewController.AnimationDirection.Horizontal : ViewController.AnimationDirection.Vertical;
        DismissViewController(view, direction, finished);
#endif
    }

#if LEGATO_BEFORE_MULTIPLAYER
    private void EventViewPresented() => EventViewPresented(null);

    private void EventViewPresented(Action? finished)
    {
        _presentingEventView = false;
        finished?.Invoke();

        // HMUI may still be unwinding the current view's transition callback.
        UnityMainThreadTaskScheduler.Factory.StartNew(FinishQueuedTransitions);
    }
#endif

    [UsedImplicitly]
    [Inject]
    private void Construct(
        SiraLog log,
        Config config,
        GameplaySetupViewController gameplaySetupViewController,
        ResultsViewController resultsViewController,
        EventDivisionSelectViewController divisionSelectViewController,
        EventIntroViewController introViewController,
        EventLeaderboardViewController leaderboardViewController,
        EventLoadingViewController loadingViewController,
        EventLobbyNavigationViewController lobbyNavigationViewController,
        EventModsDownloadingViewController modsDownloadingViewController,
        EventModsViewController modsViewController,
        SimpleDialogPromptViewController simpleDialogPromptViewController,
        CountdownManager countdownManager,
        MapDownloadingManager mapDownloadingManager,
        LevelStartManager levelStartManager,
        ListingManager listingManager,
        NetworkManager networkManager,
        MenuPrefabManager menuPrefabManager,
        GlobalParticleManager globalParticleManager)
    {
        _log = log;
        _config = config;
        _gameplaySetupViewController = gameplaySetupViewController;
        _resultsViewController = resultsViewController;
        _divisionSelectViewController = divisionSelectViewController;
        _introViewController = introViewController;
        _leaderboardViewController = leaderboardViewController;
        _loadingViewController = loadingViewController;
        _lobbyNavigationViewController = lobbyNavigationViewController;
        _modsDownloadingViewController = modsDownloadingViewController;
        _modsViewController = modsViewController;
        _simpleDialogPromptViewController = simpleDialogPromptViewController;
        _countdownManager = countdownManager;
        _mapDownloadingManager = mapDownloadingManager;
        _levelStartManager = levelStartManager;
        listingManager.ListingFound += OnListingFound;
        _networkManager = networkManager;
        _menuPrefabManager = menuPrefabManager;
        _globalParticleManager = globalParticleManager;
#if !LEGATO_DIFFICULTY_BEATMAPS
        _resultsViewControllerInit = AccessTools.Method(typeof(ResultsViewController), nameof(ResultsViewController.Init));
#endif
    }

    private void ShowLoading(bool provide)
    {
        if (_config.JoinChat == null)
        {
            _simpleDialogPromptViewController.Init(
                "Chat",
                "Automatically join chatrooms for events?",
                "Yes",
                "No",
                n =>
                {
                    _config.JoinChat = n switch
                    {
                        0 => true,
                        _ => false
                    };
                    ShowLoading(false);
                });

            if (provide)
            {
                ProvideInitialViewControllers(_simpleDialogPromptViewController);
            }
            else
            {
                TransitionFinished += () =>
                {
                    ReplaceEventView(_simpleDialogPromptViewController);
                };
            }
        }
        else if (_config.LastEvent.Division == null &&
                 _listing?.Divisions.Count > 0)
        {
            _divisionSelectViewController.SetCallback(
                n =>
                {
                    _config.LastEvent.Division = n;
                    ShowLoading(false);
                });

            if (provide)
            {
                ProvideInitialViewControllers(_divisionSelectViewController);
            }
            else
            {
                TransitionFinished += () =>
                {
                    ReplaceEventView(_divisionSelectViewController);
                };
            }
        }
        else
        {
            if (provide)
            {
                ProvideInitialViewControllers(_loadingViewController);
            }
            else
            {
                TransitionFinished += () =>
                {
                    ReplaceEventView(_loadingViewController);
                };
            }

            _ = _networkManager.RunAsync();
        }
    }

    private void HandleLevelDidFinish(
        DownloadedMap map,
        StandardLevelScenesTransitionSetupData standardLevelScenesTransitionSetupData,
        LevelCompletionResults levelCompletionResults)
    {
        TransitionDidFinish();
        _globalParticleManager.Refresh();

        // do not submit when server forces a stop
#if LEGATO_BEFORE_MULTIPLAYER
        if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.None &&
#else
        if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Incomplete &&
#endif
            levelCompletionResults.levelEndAction != LevelCompletionResults.LevelEndAction.Quit)
        {
            return;
        }

#if LEGATO_BEFORE_MULTIPLAYER
        IDifficultyBeatmap difficultyBeatmap = standardLevelScenesTransitionSetupData.Get<GameplayCoreSceneSetupData>().difficultyBeatmap;
#elif LEGATO_DIFFICULTY_BEATMAPS
        IDifficultyBeatmap difficultyBeatmap = standardLevelScenesTransitionSetupData.difficultyBeatmap;
#else
        BeatmapKey beatmapKey = standardLevelScenesTransitionSetupData.beatmapKey;
        BeatmapLevel beatmapLevel = standardLevelScenesTransitionSetupData.beatmapLevel
            ?? throw new InvalidOperationException("Completed event has no beatmap level.");
#endif
#if !LEGATO_BEFORE_MULTIPLAYER
        IReadonlyBeatmapData transformedBeatmapData = standardLevelScenesTransitionSetupData.transformedBeatmapData;
#endif
        ////this._menuLightsManager.SetColorPreset((levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared) ? this._resultsClearedLightsPreset : this._resultsFailedLightsPreset, true);
#if LEGATO_BEFORE_MULTIPLAYER
        if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.None)
        {
            levelCompletionResults.SetField(nameof(levelCompletionResults.levelEndStateType), LevelCompletionResults.LevelEndStateType.Failed);
        }
#else
        bool showFail = levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared;
        if (showFail)
        {
            levelCompletionResults.SetField(nameof(levelCompletionResults.levelEndStateType), LevelCompletionResults.LevelEndStateType.Cleared);
        }
#endif

#if LEGATO_BEFORE_MULTIPLAYER
        _resultsViewController.Init(levelCompletionResults, difficultyBeatmap, false, false);
#elif LEGATO_DIFFICULTY_BEATMAPS
        _resultsViewController.Init(
            levelCompletionResults,
            transformedBeatmapData,
            difficultyBeatmap,
            false,
            false);
#else
        // calling a method with an in readonly struct parameter seems to make github ci unhappy
        _resultsViewControllerInit.Invoke(
            _resultsViewController,
            [
                levelCompletionResults,
                transformedBeatmapData,
                beatmapKey,
                beatmapLevel,
                false,
                false
            ]);
#endif
        _resultsViewController._restartButton.gameObject.SetActive(false);
#if LEGATO_BEFORE_MULTIPLAYER
        TransitionFinished += () => PresentEventView(_resultsViewController, true, immediately: true);
#else
        TransitionFinished += () => PresentEventView(
            _resultsViewController,
            true,
            () =>
            {
                if (!showFail)
                {
                    return;
                }

                _resultsViewController._failedBannerGo.SetActive(true);
                _resultsViewController._clearedBannerGo.SetActive(false);
            },
            true);
#endif

#if BEAT_SABER_1_45_3
        _ = SubmitScore(map, levelCompletionResults.score.modified, ScorePercentageHelper.ScorePercentage);
#else
        _ = SubmitScore(map, levelCompletionResults.modifiedScore, ScorePercentageHelper.ScorePercentage);
#endif
    }

    private void HandleResultsViewControllerContinueButtonPressed(ResultsViewController viewController)
    {
        DismissEventView(
            viewController,
            true,
            () => viewController._restartButton.gameObject.SetActive(true));
    }

    private void OnAcceptModsDownload()
    {
        ReplaceEventView(_modsDownloadingViewController);
    }

    private void OnDisconnected(string reason)
    {
        TransitionFinished += () =>
        {
            if (topViewController == _simpleDialogPromptViewController)
            {
                return;
            }

            _simpleDialogPromptViewController.Init(
                "Disconnected",
                reason,
                "Ok",
                _ => { Finished?.Invoke(this); });

            ReplaceEventView(_simpleDialogPromptViewController);
        };
    }

    private void OnIntroFinished()
    {
        TransitionFinished += () =>
        {
            if (topViewController == _introViewController)
            {
                DismissEventView(_introViewController, false);
            }
        };
    }

    private void OnIntroStarted()
    {
        TransitionFinished += () =>
        {
            if (topViewController != _lobbyNavigationViewController)
            {
                return;
            }

            _introViewController.Init(true);
            PresentEventView(_introViewController);
        };
    }

    private void OnOutroStarted()
    {
        TransitionFinished += () =>
        {
            /*if (topViewController == _resultsViewController)
            {
                DismissViewController(
                    _resultsViewController,
                    ViewController.AnimationDirection.Horizontal,
                    OnOutroStarted);
                return;
            }*/

            if (topViewController != _lobbyNavigationViewController)
            {
                return;
            }

            _introViewController.Init(false);
            PresentEventView(_introViewController);
        };
    }

    private void OnLevelStarted()
    {
        TransitionFinished += () =>
        {
            if (topViewController == _resultsViewController)
            {
                DismissEventView(_resultsViewController, true, OnLevelStarted);
                return;
            }

            if (topViewController != _lobbyNavigationViewController)
            {
                return;
            }

            TransitionDidStart();
            _mapDownloadingManager.MapDownloadedOnce += n =>
            {
                if (topViewController != _lobbyNavigationViewController)
                {
                    TransitionDidFinish();
                    return;
                }

                _transitionFinished.Clear();
                try
                {
                    _levelStartManager.StartLevel(n, HandleLevelDidFinish);
                }
                catch (Exception e)
                {
                    _log.Error($"Failed to start level:\n{e}");
                    TransitionDidFinish();
                }
            };
        };
    }

    private void OnListingFound(Listing? listing)
    {
        _dirtyListing = true;
        _listing = listing;
    }

    private void OnLoadingFinished(string? error)
    {
        if (error != null)
        {
            OnDisconnected(error);
            return;
        }

        TransitionFinished += () =>
        {
            if (topViewController == _loadingViewController)
            {
                ReplaceEventView(_lobbyNavigationViewController);
            }
        };
    }

    private async Task SubmitScore(DownloadedMap map, int score, float percentage)
    {
        int division = _config.LastEvent.Division ?? 0;
        int index = map.Index;
        ScoreSubmission scoreSubmission = new()
        {
            Division = division,
            Index = index,
            Score = score,
            Percentage = percentage
        };
        string scoreJson = JsonConvert.SerializeObject(scoreSubmission, JsonSettings.Settings);
        _leaderboardViewController.ChangeSelection(index);
        int submissionTry = 0;
        while (_networkManager.Running)
        {
            await _networkManager.Send(ServerOpcode.ScoreSubmission, scoreJson);
            await Task.Delay(SCORE_SUBMISSION_INTERVAL);
            if (++submissionTry >= SCORE_SUBMISSION_ATTEMPTS)
            {
                break;
            }

            if (_networkManager.AcknowledgedScores.TryGetValue(index, out int acknowledgedScore) &&
                acknowledgedScore == score)
            {
                return;
            }
        }

        _log.Error($"Failed to submit score for [{map.Map.Name}], attempted to submit [{submissionTry}] times");
    }

    internal class EventFlowCoordinatorFactory : IFactory<EventFlowCoordinator>
    {
        private readonly IInstantiator _instantiator;
        private readonly MainFlowCoordinator _mainFlowCoordinator;

        [UsedImplicitly]
        private EventFlowCoordinatorFactory(IInstantiator instantiator, MainFlowCoordinator mainFlowCoordinator)
        {
            _instantiator = instantiator;
            _mainFlowCoordinator = mainFlowCoordinator;
        }

        public EventFlowCoordinator Create()
        {
            GameObject gameObject = new(nameof(EventFlowCoordinator));
            gameObject.transform.SetParent(_mainFlowCoordinator.transform.parent);
            gameObject.layer = _mainFlowCoordinator.gameObject.layer;

            return _instantiator.InstantiateComponent<EventFlowCoordinator>(gameObject);
        }
    }
}
