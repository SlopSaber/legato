#if LEGATO_HECK_LEGACY_GAMEPLAY
using System;

namespace Heck.PlayView;

public class StartStandardLevelParameters
{
    public StartStandardLevelParameters(
        string gameMode,
        IDifficultyBeatmap difficultyBeatmap,
        OverrideEnvironmentSettings? overrideEnvironmentSettings,
        ColorScheme? overrideColorScheme,
        GameplayModifiers gameplayModifiers,
        PlayerSpecificSettings playerSpecificSettings,
        PracticeSettings? practiceSettings,
        string backButtonText,
        bool useTestNoteCutSoundEffects,
        Action? beforeSceneSwitchCallback,
        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelFinishedCallback)
    {
        GameMode = gameMode;
        DifficultyBeatmap = difficultyBeatmap;
        OverrideEnvironmentSettings = overrideEnvironmentSettings;
        OverrideColorScheme = overrideColorScheme;
        GameplayModifiers = gameplayModifiers;
        PlayerSpecificSettings = playerSpecificSettings;
        PracticeSettings = practiceSettings;
        BackButtonText = backButtonText;
        UseTestNoteCutSoundEffects = useTestNoteCutSoundEffects;
        BeforeSceneSwitchCallback = beforeSceneSwitchCallback;
        LevelFinishedCallback = levelFinishedCallback;
    }

    public string GameMode { get; }

    public IDifficultyBeatmap DifficultyBeatmap { get; }

    public IPreviewBeatmapLevel PreviewBeatmapLevel => DifficultyBeatmap.level;

    public OverrideEnvironmentSettings? OverrideEnvironmentSettings { get; set; }

    public ColorScheme? OverrideColorScheme { get; set; }

    public GameplayModifiers GameplayModifiers { get; set; }

    public PlayerSpecificSettings PlayerSpecificSettings { get; set; }

    public PracticeSettings? PracticeSettings { get; }

    public string BackButtonText { get; }

    public bool UseTestNoteCutSoundEffects { get; }

    public Action? BeforeSceneSwitchCallback { get; }

    public Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? LevelFinishedCallback { get; }

    public virtual StartStandardLevelParameters Copy() => (StartStandardLevelParameters)MemberwiseClone();
}
#endif
