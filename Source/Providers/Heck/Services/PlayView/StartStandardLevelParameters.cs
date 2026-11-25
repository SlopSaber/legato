using System;
using JetBrains.Annotations;
#if !PRE_V1_37_1
using Zenject;
#endif

namespace Heck.PlayView;

public class StartStandardLevelParameters
{
    [UsedImplicitly]
    public StartStandardLevelParameters(
        string gameMode,
#if !PRE_V1_37_1
        in BeatmapKey beatmapKey,
        BeatmapLevel beatmapLevel,
#else
        IDifficultyBeatmap difficultyBeatmap,
        IPreviewBeatmapLevel previewBeatmapLevel,
#endif
        OverrideEnvironmentSettings? overrideEnvironmentSettings,
        ColorScheme? overrideColorScheme,
#if !PRE_V1_40_8
        bool playerOverrideLightshowColors,
#endif
#if !V1_29_1 && !LATEST
        ColorScheme? beatmapOverrideColorScheme,
#endif
        GameplayModifiers gameplayModifiers,
        PlayerSpecificSettings playerSpecificSettings,
        PracticeSettings? practiceSettings,
#if !PRE_V1_37_1
        EnvironmentsListModel? environmentsListModel,
#endif
#if LATEST
        GameplayAdditionalInformation? gameplayAdditionalInformation,
        Action? beforeSceneSwitchToGameplayCallback,
        Action<DiContainer>? afterSceneSwitchToGameplayCallback,
#else
        string backButtonText,
        bool useTestNoteCutSoundEffects,
        bool startPaused,
        Action? beforeSceneSwitchCallback,
#endif
#if !PRE_V1_37_1 && !LATEST
        Action<DiContainer>? afterSceneSwitchCallback,
#endif
        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelFinishedCallback,
#if V1_29_1
        Action<LevelScenesTransitionSetupData, LevelCompletionResults>? levelRestartedCallback)
#else
        Action<LevelScenesTransitionSetupData, LevelCompletionResults>? levelRestartedCallback,
#if LATEST
        IBeatmapLevelData? beatmapLevelData)
#else
        RecordingToolManager.SetupData? recordingToolData)
#endif
#endif
    {
        GameMode = gameMode;
#if !PRE_V1_37_1
        BeatmapKey = beatmapKey;
        BeatmapLevel = beatmapLevel;
#else
        DifficultyBeatmap = difficultyBeatmap;
        PreviewBeatmapLevel = previewBeatmapLevel;
#endif
        OverrideEnvironmentSettings = overrideEnvironmentSettings;
        OverrideColorScheme = overrideColorScheme;
#if !PRE_V1_40_8
        PlayerOverrideLightshowColors = playerOverrideLightshowColors;
#endif
#if !V1_29_1 && !LATEST
        BeatmapOverrideColorScheme = beatmapOverrideColorScheme;
#endif
        GameplayModifiers = gameplayModifiers;
        PlayerSpecificSettings = playerSpecificSettings;
        PracticeSettings = practiceSettings;
#if !PRE_V1_37_1
        EnvironmentsListModel = environmentsListModel;
#endif
#if LATEST
        GameplayAdditionalInformation = gameplayAdditionalInformation;
        BeforeSceneSwitchCallback = beforeSceneSwitchToGameplayCallback;
        AfterSceneSwitchCallback = afterSceneSwitchToGameplayCallback;
#else
        BackButtonText = backButtonText;
        UseTestNoteCutSoundEffects = useTestNoteCutSoundEffects;
        StartPaused = startPaused;
        BeforeSceneSwitchCallback = beforeSceneSwitchCallback;
#if !PRE_V1_37_1
        AfterSceneSwitchCallback = afterSceneSwitchCallback;
#endif
#endif
        LevelFinishedCallback = levelFinishedCallback;
        LevelRestartedCallback = levelRestartedCallback;
#if !V1_29_1 && !LATEST
        RecordingToolData = recordingToolData;
#endif
#if LATEST
        BeatmapLevelData = beatmapLevelData;
#endif
    }

    public StartStandardLevelParameters(StartStandardLevelParameters original)
    {
        GameMode = original.GameMode;
#if !PRE_V1_37_1
        BeatmapKey = original.BeatmapKey;
        BeatmapLevel = original.BeatmapLevel;
#else
        DifficultyBeatmap = original.DifficultyBeatmap;
        PreviewBeatmapLevel = original.PreviewBeatmapLevel;
#endif
        OverrideEnvironmentSettings = original.OverrideEnvironmentSettings;
        OverrideColorScheme = original.OverrideColorScheme;
#if !PRE_V1_40_8
        PlayerOverrideLightshowColors = original.PlayerOverrideLightshowColors;
#endif
#if !V1_29_1 && !LATEST
        BeatmapOverrideColorScheme = original.BeatmapOverrideColorScheme;
#endif
        GameplayModifiers = original.GameplayModifiers;
        PlayerSpecificSettings = original.PlayerSpecificSettings;
        PracticeSettings = original.PracticeSettings;
#if !PRE_V1_37_1
        EnvironmentsListModel = original.EnvironmentsListModel;
#endif
#if LATEST
        GameplayAdditionalInformation = original.GameplayAdditionalInformation;
#else
        BackButtonText = original.BackButtonText;
        UseTestNoteCutSoundEffects = original.UseTestNoteCutSoundEffects;
        StartPaused = original.StartPaused;
#endif
#if !PRE_V1_37_1
        AfterSceneSwitchCallback = original.AfterSceneSwitchCallback;
#endif
        BeforeSceneSwitchCallback = original.BeforeSceneSwitchCallback;
        LevelFinishedCallback = original.LevelFinishedCallback;
        LevelRestartedCallback = original.LevelRestartedCallback;
#if !V1_29_1 && !LATEST
        RecordingToolData = original.RecordingToolData;
#endif
#if LATEST
        BeatmapLevelData = original.BeatmapLevelData;
#if LEGATO_SCRIPTABLE_SCENE_TRANSITIONS
        RecordingToolData = original.RecordingToolData;
#endif
#endif
    }

    public string GameMode { get; }

#if !PRE_V1_37_1
    public BeatmapKey BeatmapKey { get; }

    public BeatmapLevel BeatmapLevel { get; }
#else
    public IDifficultyBeatmap DifficultyBeatmap { get; }

    public IPreviewBeatmapLevel PreviewBeatmapLevel { get; }
#endif

    public OverrideEnvironmentSettings? OverrideEnvironmentSettings { get; set; }

    public ColorScheme? OverrideColorScheme { get; set; }

#if !PRE_V1_40_8
    public bool PlayerOverrideLightshowColors { get; }
#endif

#if !V1_29_1 && !LATEST
    public ColorScheme? BeatmapOverrideColorScheme { get; }
#endif

    public GameplayModifiers GameplayModifiers { get; set; }

    public PlayerSpecificSettings PlayerSpecificSettings { get; set; }

    public PracticeSettings? PracticeSettings { get; }

#if !PRE_V1_37_1
    public EnvironmentsListModel? EnvironmentsListModel { get; }
#endif

#if LATEST
    public GameplayAdditionalInformation? GameplayAdditionalInformation { get; }
#else
    public string BackButtonText { get; }

    public bool UseTestNoteCutSoundEffects { get; }

    public bool StartPaused { get; }
#endif

    public Action? BeforeSceneSwitchCallback { get; }

#if !PRE_V1_37_1
    public Action<DiContainer>? AfterSceneSwitchCallback { get; }
#endif

    public Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? LevelFinishedCallback { get; }

    public Action<LevelScenesTransitionSetupData, LevelCompletionResults>? LevelRestartedCallback { get; }

#if !V1_29_1 && !LATEST
    public RecordingToolManager.SetupData? RecordingToolData { get; }
#endif

#if LATEST
    public IBeatmapLevelData? BeatmapLevelData { get; }
#if LEGATO_SCRIPTABLE_SCENE_TRANSITIONS

    public RecordingToolManager.SetupData? RecordingToolData { get; private set; }
#endif
#endif

    public virtual StartStandardLevelParameters Copy()
    {
        return new StartStandardLevelParameters(this);
    }

#if LEGATO_STANDARD_LEVEL_RESTART_CALLBACK
    internal static StartStandardLevelParameters FromGame(
#if BEAT_SABER_1453
        global::GameMode gameMode,
#else
        string gameMode,
#endif
        in BeatmapKey beatmapKey,
        BeatmapLevel beatmapLevel,
        OverrideEnvironmentSettings? overrideEnvironmentSettings,
        ColorScheme? overrideColorScheme,
#if !LEGATO_SINGLE_COLOR_OVERRIDE
        bool playerOverrideLightshowColors,
#endif
#if !LATEST
        ColorScheme? beatmapOverrideColorScheme,
#endif
        GameplayModifiers gameplayModifiers,
        PlayerSpecificSettings playerSpecificSettings,
        PracticeSettings? practiceSettings,
        EnvironmentsListModel? environmentsListModel,
#if LATEST
        GameplayAdditionalInformation? gameplayAdditionalInformation,
#else
        string backButtonText,
        bool useTestNoteCutSoundEffects,
        bool startPaused,
#endif
        Action? beforeSceneSwitchToGameplayCallback,
        Action<DiContainer>? afterSceneSwitchToGameplayCallback,
        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelFinishedCallback,
        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelRestartedCallback,
#if LATEST
#if LEGATO_SCRIPTABLE_SCENE_TRANSITIONS
        IBeatmapLevelData? beatmapLevelData,
        RecordingToolManager.SetupData? recordingToolData)
#else
        IBeatmapLevelData? beatmapLevelData)
#endif
#else
        RecordingToolManager.SetupData? recordingToolData)
#endif
    {
#if BEAT_SABER_1453
        string gameModeName = gameMode.ToString();
#else
        string gameModeName = gameMode;
#endif
        Action<LevelScenesTransitionSetupData, LevelCompletionResults>? restart = levelRestartedCallback == null
            ? null
            : (transition, results) => levelRestartedCallback((StandardLevelScenesTransitionSetupData)transition, results);
        var parameters = new StartStandardLevelParameters(
            gameModeName,
            in beatmapKey,
            beatmapLevel,
            overrideEnvironmentSettings,
            overrideColorScheme,
#if !LEGATO_SINGLE_COLOR_OVERRIDE
            playerOverrideLightshowColors,
#endif
#if !LATEST
            beatmapOverrideColorScheme,
#endif
            gameplayModifiers,
            playerSpecificSettings,
            practiceSettings,
            environmentsListModel,
#if LATEST
            gameplayAdditionalInformation,
#else
            backButtonText,
            useTestNoteCutSoundEffects,
            startPaused,
#endif
            beforeSceneSwitchToGameplayCallback,
            afterSceneSwitchToGameplayCallback,
            levelFinishedCallback,
            restart,
#if LATEST
            beatmapLevelData);
#if LEGATO_SCRIPTABLE_SCENE_TRANSITIONS
        parameters.RecordingToolData = recordingToolData;
#endif
#else
            recordingToolData);
#endif
        return parameters;
    }
#endif
}
