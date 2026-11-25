using System;
using System.Reflection;
using HarmonyLib;
using IPA.Loader;
using JetBrains.Annotations;
using Zenject;

namespace Synapse.Managers;

internal class HeckIntegrationManager
{
    private readonly object _playViewManager;

    private readonly ConstructorInfo _parametersConstructor;
    private readonly MethodInfo _forceStart;

    [UsedImplicitly]
    private HeckIntegrationManager(DiContainer container)
    {
        Assembly assembly = PluginManager.GetPlugin("Heck").Assembly;
        Type? playViewManagerType = assembly.GetType("Heck.PlayView.PlayViewManager");
        if (playViewManagerType == null)
        {
            throw new InvalidOperationException("Failed to get Heck.PlayView.PlayViewManager type");
        }

        _playViewManager = container.Resolve(playViewManagerType);
        _forceStart = AccessTools.Method(playViewManagerType, "ForceStart");
        Type? startStandardLevelParametersType = assembly.GetType("Heck.PlayView.StartStandardLevelParameters");
        if (startStandardLevelParametersType == null)
        {
            throw new InvalidOperationException("Failed to get Heck.PlayView.StartStandardLevelParameters type");
        }

        _parametersConstructor = AccessTools.FirstConstructor(
            startStandardLevelParametersType,
            n => n.GetParameters().Length > 1);
    }

    internal void StartPlayViewInterruptedLevel(
        string gameMode,
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
        object parameters = _parametersConstructor.Invoke([
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
            beatmapLevelData,
#else
            recordingToolData
#endif
#else
            levelRestartedCallback
#endif
        ]);
        _forceStart.Invoke(_playViewManager, [parameters]);
    }
}
