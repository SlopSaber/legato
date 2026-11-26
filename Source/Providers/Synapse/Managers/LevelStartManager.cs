using System;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;
using JetBrains.Annotations;
using SiraUtil.Logging;
using Synapse.HarmonyPatches;
using Synapse.Models;
using Synapse.Networking.Models;
using Zenject;

namespace Synapse.Managers;

internal class LevelStartManager : IDisposable
{
    private static readonly Action<string>? _nextLevelIsIsolated = GetNextLevelIsIsolated();

    private readonly Action? _disableCustomPlatform;
#if !LEGATO_DIFFICULTY_BEATMAPS
    private readonly EnvironmentsListModel _environmentsListModel;
#endif

    private readonly Config _config;
    private readonly GameplaySetupViewController _gameplaySetupViewController;
    private readonly HeckIntegrationManager? _heckIntegrationManager;
    private readonly MenuTransitionsHelper _menuTransitionsHelper;
    private readonly NetworkManager _networkManager;
    private readonly NoEnergyModifier _noEnergyModifier;

    private Ruleset? _ruleset;

    [UsedImplicitly]
    private LevelStartManager(
        SiraLog log,
        Config config,
        DiContainer container,
        GameplaySetupViewController gameplaySetupViewController,
        MenuTransitionsHelper menuTransitionsHelper,
#if !LEGATO_DIFFICULTY_BEATMAPS
        EnvironmentsListModel environmentsListModel,
#endif
        NetworkManager networkManager,
        NoEnergyModifier noEnergyModifier,
        [InjectOptional] HeckIntegrationManager? heckIntegrationManager)
    {
        _config = config;
        _gameplaySetupViewController = gameplaySetupViewController;
        _menuTransitionsHelper = menuTransitionsHelper;
#if !LEGATO_DIFFICULTY_BEATMAPS
        _environmentsListModel = environmentsListModel;
#endif
        _networkManager = networkManager;
        _noEnergyModifier = noEnergyModifier;
        _heckIntegrationManager = heckIntegrationManager;
        networkManager.MapUpdated += OnMapUpdated;

        // to disable custom platforms
        PluginMetadata? customPlatforms = PluginManager.GetPlugin("Custom Platforms");

        // ReSharper disable once InvertIf
        if (customPlatforms != null)
        {
            // cannot use ConnectionManager as it is not bound
            Type? platformManagerType = customPlatforms.Assembly.GetType("CustomFloorPlugin.PlatformManager");
            if (platformManagerType != null)
            {
                object? platformsConnectionManager = container.TryResolve(platformManagerType);
                if (platformsConnectionManager != null)
                {
                    MethodInfo? setPlatform = platformManagerType
                        .GetProperty(
                            "APIRequestedPlatform",
                            AccessTools.all)
                        ?.GetSetMethod(true);
                    MethodInfo? getDefault = platformManagerType
                        .GetProperty(
                            "DefaultPlatform",
                            AccessTools.all)
                        ?.GetGetMethod();
                    if (setPlatform == null)
                    {
                        log.Error("Could not find [CustomFloorPlugin.PlatformManager.APIRequestedPlatform] setter");
                    }
                    else if (getDefault == null)
                    {
                        log.Error("Could not find [CustomFloorPlugin.PlatformManager.DefaultPlatform] getter");
                    }
                    else
                    {
                        object defaultPlatform = getDefault.Invoke(platformsConnectionManager, null);
                        _disableCustomPlatform = () => setPlatform.Invoke(
                            platformsConnectionManager,
                            [defaultPlatform]);
                    }
                }
                else
                {
                    log.Error("Could not resolve [CustomFloorPlugin.PlatformManager] instance");
                }
            }
            else
            {
                log.Error("Could not find [CustomFloorPlugin.ConnectionManager] type");
            }
        }
    }

#pragma warning disable SA1300
    // ReSharper disable InconsistentNaming
    private enum GameplayModifier
    {
        noFailOn0Energy,
        instaFail,
        failOnSaberClash,
        noBombs,
        fastNotes,
        strictAngles,
        disappearingArrows,
        noArrows,
        ghostNotes,
        proMode,
        zenMode,
        smallCubes,
        noEnergy // custom modifier
    }
#pragma warning restore SA1300

    public void Dispose()
    {
        _networkManager.MapUpdated -= OnMapUpdated;
    }

    // WARNING: ruleset has lower priority than heck map settings
#if LEGATO_BEFORE_MULTIPLAYER
    public void StartLevel(
        DownloadedMap downloadedMap,
        Action<DownloadedMap, StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelFinishedCallback)
    {
        var schemes = _gameplaySetupViewController.colorSchemesSettings;
        ColorScheme? overrideColorScheme = _ruleset?.AllowOverrideColors != false && schemes.overrideDefaultColors
            ? schemes.GetSelectedColorScheme()
            : null;
        PlayerSpecificSettings settings = _gameplaySetupViewController.playerSettings;
        if (_ruleset?.AllowLeftHand == false && settings.leftHanded)
        {
            settings = new PlayerSpecificSettings
            {
                leftHanded = false,
                staticLights = settings.staticLights,
                playerHeight = settings.playerHeight,
                automaticPlayerHeight = settings.automaticPlayerHeight,
                sfxVolume = settings.sfxVolume,
                reduceDebris = settings.reduceDebris,
                noTextsAndHuds = settings.noTextsAndHuds,
                noFailEffects = settings.noFailEffects,
                advancedHud = settings.advancedHud,
                autoRestart = settings.autoRestart,
                saberTrailIntensity = settings.saberTrailIntensity
            };
        }

        GameplayModifiers modifiers = new();
        bool noEnergy = false;
        if (_ruleset?.Modifiers is { } rulesetModifiers)
        {
            foreach (string name in rulesetModifiers)
            {
                if (!Enum.TryParse(name, true, out GameplayModifier modifier))
                {
                    continue;
                }

                switch (modifier)
                {
                    case GameplayModifier.noFailOn0Energy:
                        modifiers.noFail = true;
                        break;
                    case GameplayModifier.instaFail:
                        modifiers.instaFail = true;
                        break;
                    case GameplayModifier.failOnSaberClash:
                        modifiers.failOnSaberClash = true;
                        break;
                    case GameplayModifier.noBombs:
                        modifiers.noBombs = true;
                        break;
                    case GameplayModifier.fastNotes:
                        modifiers.fastNotes = true;
                        break;
                    case GameplayModifier.strictAngles:
                        modifiers.strictAngles = true;
                        break;
                    case GameplayModifier.disappearingArrows:
                        modifiers.disappearingArrows = true;
                        break;
                    case GameplayModifier.noArrows:
                        modifiers.noArrows = true;
                        break;
                    case GameplayModifier.ghostNotes:
                        modifiers.ghostNotes = true;
                        break;
                    case GameplayModifier.noEnergy:
                        noEnergy = true;
                        break;
                    default:
                        throw new NotSupportedException($"Event modifier '{name}' is unavailable in this game version.");
                }
            }
        }

        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? callback = levelFinishedCallback == null
            ? null
            : (transition, results) => levelFinishedCallback(downloadedMap, transition, results);
        int division = _config.LastEvent.Division ?? 0;
        IDifficultyBeatmap difficulty = downloadedMap.DifficultyBeatmaps[division];
        _disableCustomPlatform?.Invoke();
        _nextLevelIsIsolated?.Invoke("Synapse");
        IntroSkipInstallationPatch.SkipNext = true;
        _noEnergyModifier.NoEnergyNextMap = noEnergy;
        try
        {
            if (_heckIntegrationManager != null)
            {
                _heckIntegrationManager.StartPlayViewInterruptedLevel(difficulty, overrideColorScheme, modifiers, settings, callback);
            }
            else
            {
                _menuTransitionsHelper.StartStandardLevel(
                    difficulty,
                    null,
                    overrideColorScheme,
                    modifiers,
                    settings,
                    null,
                    string.Empty,
                    false,
                    null,
                    callback);
            }
        }
        catch
        {
            IntroSkipInstallationPatch.SkipNext = false;
            _noEnergyModifier.NoEnergyNextMap = false;
            throw;
        }
    }
#else
    public void StartLevel(
        DownloadedMap downloadedMap,
        Action<DownloadedMap, StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelFinishedCallback)
    {
        ColorScheme? overrideColorScheme;
        if (_ruleset?.AllowOverrideColors != null && !_ruleset.AllowOverrideColors.Value)
        {
            overrideColorScheme = null;
        }
        else
        {
            overrideColorScheme = _gameplaySetupViewController.colorSchemesSettings.GetOverrideColorScheme();
        }

        PlayerSpecificSettings playerSpecificSettings;
        if (_ruleset?.AllowLeftHand != null && !_ruleset.AllowLeftHand.Value)
        {
            playerSpecificSettings = _gameplaySetupViewController.playerSettings.CopyWith();
            playerSpecificSettings._leftHanded = false;
        }
        else
        {
            playerSpecificSettings = _gameplaySetupViewController.playerSettings;
        }

        GameplayModifiers modifiers = new();
        if (_ruleset is { Modifiers: not null })
        {
            foreach (string rulesetModifier in _ruleset.Modifiers)
            {
                if (!Enum.TryParse(rulesetModifier, true, out GameplayModifier modifier))
                {
                    continue;
                }

                switch (modifier)
                {
                    case GameplayModifier.noFailOn0Energy:
                        modifiers._noFailOn0Energy = true;
                        break;

                    case GameplayModifier.instaFail:
                        modifiers._instaFail = true;
                        break;

                    case GameplayModifier.failOnSaberClash:
                        modifiers._failOnSaberClash = true;
                        break;

                    case GameplayModifier.noBombs:
                        modifiers._noBombs = true;
                        break;

                    case GameplayModifier.fastNotes:
                        modifiers._fastNotes = true;
                        break;

                    case GameplayModifier.strictAngles:
                        modifiers._strictAngles = true;
                        break;

                    case GameplayModifier.disappearingArrows:
                        modifiers._disappearingArrows = true;
                        break;

                    case GameplayModifier.noArrows:
                        modifiers._noArrows = true;
                        break;

                    case GameplayModifier.ghostNotes:
                        modifiers._ghostNotes = true;
                        break;

                    case GameplayModifier.proMode:
                        modifiers._proMode = true;
                        break;

                    case GameplayModifier.zenMode:
                        modifiers._zenMode = true;
                        break;

                    case GameplayModifier.smallCubes:
                        modifiers._smallCubes = true;
                        break;

                    case GameplayModifier.noEnergy:
                        _noEnergyModifier.NoEnergyNextMap = true;
                        break;
                }
            }
        }

        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? callback = null;
        if (levelFinishedCallback != null)
        {
            callback = (a, b) => levelFinishedCallback(downloadedMap, a, b);
        }

        int division = _config.LastEvent.Division ?? 0;

#if !LEGATO_DIFFICULTY_BEATMAPS
        BeatmapKey beatmapKey = downloadedMap.BeatmapKeys[division];
        BeatmapLevel beatmapLevel = downloadedMap.BeatmapLevel;
#if LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
        ColorScheme? beatmapOverrideColorScheme =
            beatmapLevel.GetColorScheme(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
#endif
#elif !BEAT_SABER_1_29_0
        ColorScheme? beatmapOverrideColorScheme = null;
        if (downloadedMap is { BeatmapLevel: CustomBeatmapLevel customBeatmapLevel } &&
            downloadedMap.DifficultyBeatmaps[division] is CustomDifficultyBeatmap customDifficultyBeatmap)
        {
            beatmapOverrideColorScheme =
                customBeatmapLevel.GetBeatmapLevelColorScheme(customDifficultyBeatmap.beatmapColorSchemeIdx);
        }
#endif

        _disableCustomPlatform?.Invoke();
        _nextLevelIsIsolated?.Invoke("Synapse");
        IntroSkipInstallationPatch.SkipNext = true;

#if BEAT_SABER_1_45_3
        const GameMode selectedGameMode = GameMode.Development;
#else
        const string selectedGameMode = "screw yo analytics";
#endif
        StartStandardOrHeck(
            selectedGameMode,
#if !LEGATO_DIFFICULTY_BEATMAPS
            beatmapKey,
            beatmapLevel,
#else
            downloadedMap.DifficultyBeatmaps[division],
            downloadedMap.BeatmapLevel,
#endif
            null, // no environment override
            overrideColorScheme,
#if !LEGATO_SINGLE_COLOR_OVERRIDE
            _gameplaySetupViewController.colorSchemesSettings.ShouldOverrideLightshowColors(),
#endif
#if !BEAT_SABER_1_29_0 && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
            beatmapOverrideColorScheme,
#endif
            modifiers,
            playerSpecificSettings,
            null,
#if !LEGATO_DIFFICULTY_BEATMAPS
            _environmentsListModel,
#endif
#if !LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
            new GameplayAdditionalInformation(string.Empty),
            null,
            null,
#else
            string.Empty, // doesnt matter, gets reset by animation anyways
            false,
            false,
            null,
#endif
#if !LEGATO_DIFFICULTY_BEATMAPS && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
            null,
#endif
            callback,
#if !BEAT_SABER_1_29_0
            null,
#endif
            null);
    }
#endif

    private static Action<string>? GetNextLevelIsIsolated()
    {
        // to disable introskip
        PluginMetadata? bsUtils = PluginManager.GetPlugin("BS_Utils");
        if (bsUtils == null)
        {
            return null;
        }

        Type? type = bsUtils.Assembly.GetType("BS_Utils.Gameplay.Gamemode");
        MethodInfo? method = type?.GetMethod("NextLevelIsIsolated", BindingFlags.Static | BindingFlags.Public);
        if (type == null)
        {
            Plugin.Log.Error("Could not find [BS_Utils.Gameplay.Gamemode] type");
        }
        else if (method == null)
        {
            Plugin.Log.Error("Could not find [BS_Utils.Gameplay.Gamemode.NextLevelIsIsolated] method");
        }
        else
        {
            return (Action<string>?)method.CreateDelegate(typeof(Action<string>), null);
        }

        return null;
    }

    private void OnMapUpdated(int _, Map map)
    {
        _ruleset = map.Ruleset;
    }

    // i wish i could use my StartStandardLevelParameters here
#if !LEGATO_BEFORE_MULTIPLAYER
    private void StartStandardOrHeck(
#if BEAT_SABER_1_45_3
        GameMode gameMode,
#else
        string gameMode,
#endif
#if !LEGATO_DIFFICULTY_BEATMAPS
        in BeatmapKey beatmapKey,
        BeatmapLevel beatmapLevel,
#else
        IDifficultyBeatmap difficultyBeatmap,
        IPreviewBeatmapLevel previewBeatmapLevel,
#endif
        OverrideEnvironmentSettings? overrideEnvironmentSettings,
        ColorScheme? overrideColorScheme,
#if !LEGATO_SINGLE_COLOR_OVERRIDE
        bool playerOverrideLightshowColors,
#endif
#if !BEAT_SABER_1_29_0 && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
        ColorScheme? beatmapOverrideColorScheme,
#endif
        GameplayModifiers gameplayModifiers,
        PlayerSpecificSettings playerSpecificSettings,
        PracticeSettings? practiceSettings,
#if !LEGATO_DIFFICULTY_BEATMAPS
        EnvironmentsListModel environmentsListModel,
#endif
#if !LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
        GameplayAdditionalInformation gameplayAdditionalInformation,
        Action? beforeSceneSwitchToGameplayCallback,
        Action<DiContainer>? afterSceneSwitchToGameplayCallback,
#else
        string backButtonText,
        bool useTestNoteCutSoundEffects,
        bool startPaused,
        Action? beforeSceneSwitchCallback,
#endif
#if !LEGATO_DIFFICULTY_BEATMAPS && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
        Action<DiContainer>? afterSceneSwitchCallback,
#endif
        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelFinishedCallback,
#if !BEAT_SABER_1_29_0
        Action<LevelScenesTransitionSetupData, LevelCompletionResults>? levelRestartedCallback,
#if !LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
        IBeatmapLevelData? beatmapLevelData)
#else
        RecordingToolManager.SetupData? recordingToolData)
#endif
#else
        Action<LevelScenesTransitionSetupDataSO, LevelCompletionResults>? levelRestartedCallback)
#endif
    {
        if (_heckIntegrationManager != null)
        {
#if BEAT_SABER_1_45_3
            string heckGameMode = gameMode.ToString();
#else
            string heckGameMode = gameMode;
#endif
            _heckIntegrationManager.StartPlayViewInterruptedLevel(
                heckGameMode,
#if !LEGATO_DIFFICULTY_BEATMAPS
                beatmapKey,
                beatmapLevel,
#else
                difficultyBeatmap,
                previewBeatmapLevel,
#endif
                overrideEnvironmentSettings,
                overrideColorScheme,
#if !LEGATO_SINGLE_COLOR_OVERRIDE
                playerOverrideLightshowColors,
#endif
#if !BEAT_SABER_1_29_0 && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                beatmapOverrideColorScheme,
#endif
                gameplayModifiers,
                playerSpecificSettings,
                practiceSettings,
#if !LEGATO_DIFFICULTY_BEATMAPS
                environmentsListModel,
#endif
#if !LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                gameplayAdditionalInformation,
                beforeSceneSwitchToGameplayCallback,
                afterSceneSwitchToGameplayCallback,
#else
                backButtonText,
                useTestNoteCutSoundEffects,
                startPaused,
                beforeSceneSwitchCallback,
#endif
#if !LEGATO_DIFFICULTY_BEATMAPS && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                afterSceneSwitchCallback,
#endif
                levelFinishedCallback,
#if !BEAT_SABER_1_29_0
                levelRestartedCallback,
#if !LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                beatmapLevelData);
#else
                recordingToolData);
#endif
#else
                levelRestartedCallback);
#endif
        }
        else
        {
            _menuTransitionsHelper.StartStandardLevel(
                gameMode,
#if !LEGATO_DIFFICULTY_BEATMAPS
                beatmapKey,
                beatmapLevel,
#else
                difficultyBeatmap,
                previewBeatmapLevel,
#endif
                overrideEnvironmentSettings,
                overrideColorScheme,
#if !LEGATO_SINGLE_COLOR_OVERRIDE
                playerOverrideLightshowColors,
#endif
#if !BEAT_SABER_1_29_0 && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                beatmapOverrideColorScheme,
#endif
                gameplayModifiers,
                playerSpecificSettings,
                practiceSettings,
#if !LEGATO_DIFFICULTY_BEATMAPS
                environmentsListModel,
#endif
#if !LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                gameplayAdditionalInformation,
                beforeSceneSwitchToGameplayCallback,
                afterSceneSwitchToGameplayCallback,
#else
                backButtonText,
                useTestNoteCutSoundEffects,
                startPaused,
                beforeSceneSwitchCallback,
#endif
#if !LEGATO_DIFFICULTY_BEATMAPS && LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                afterSceneSwitchCallback,
#endif
                levelFinishedCallback,
#if !BEAT_SABER_1_29_0
                levelRestartedCallback,
#if !LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
                beatmapLevelData);
#else
                recordingToolData);
#endif
#else
                levelRestartedCallback);
#endif
        }
    }
#endif
}
