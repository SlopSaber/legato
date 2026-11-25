#if LEGATO_HECK_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck.PlayView;
using HMUI;
using SiraUtil.Affinity;

namespace Heck.HarmonyPatches;

internal sealed class PlayViewInterrupter : IAffinity, IDisposable
{
    private static readonly MethodInfo _startLevel = AccessTools.Method(
        typeof(MenuTransitionsHelper),
        nameof(MenuTransitionsHelper.StartStandardLevel),
        new[]
        {
            typeof(IDifficultyBeatmap), typeof(OverrideEnvironmentSettings), typeof(ColorScheme), typeof(GameplayModifiers),
            typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool), typeof(Action),
            typeof(Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>)
        });

    private readonly PlayViewManager _playViewManager;
    private readonly CodeInstruction _interceptStart;

    private PlayViewInterrupter(PlayViewManager playViewManager)
    {
        _playViewManager = playViewManager;
        _interceptStart = InstanceTranspilers.EmitInstanceDelegate<Action<MenuTransitionsHelper, IDifficultyBeatmap,
            OverrideEnvironmentSettings?, ColorScheme?, GameplayModifiers, PlayerSpecificSettings, PracticeSettings?, string,
            bool, Action?, Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>?, LevelSelectionFlowCoordinator>>(InterceptStart);
    }

    public void Dispose() => InstanceTranspilers.DisposeDelegate(_interceptStart);

    [AffinityTranspiler]
    [AffinityPatch(typeof(LevelSelectionFlowCoordinator), "StartLevel")]
    private IEnumerable<CodeInstruction> RedirectStart(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Callvirt && Equals(instruction.operand, _startLevel))
            {
                var loadCoordinator = new CodeInstruction(OpCodes.Ldarg_0);
                loadCoordinator.labels.AddRange(instruction.labels);
                loadCoordinator.blocks.AddRange(instruction.blocks);
                yield return loadCoordinator;
                yield return new CodeInstruction(_interceptStart.opcode, _interceptStart.operand);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(LevelSelectionFlowCoordinator), "TopViewControllerWillChange")]
    private bool PreparePlayView(LevelSelectionFlowCoordinator __instance, ViewController newViewController, bool immediately)
    {
        if (_playViewManager.ActiveView?.ViewController is not ViewController activeView || newViewController != activeView)
        {
            return true;
        }

        __instance.SetLeftScreenViewController(null, immediately);
        __instance.SetRightScreenViewController(null, immediately);
        __instance.SetBottomScreenViewController(null, immediately);
        __instance.showBackButton = true;
        return false;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(LevelSelectionFlowCoordinator), "BackButtonWasPressed")]
    private bool BackButtonWasPressed() => _playViewManager.EarlyDismiss();

    [AffinityPrefix]
    [AffinityPatch(typeof(MenuTransitionsHelper), "HandleMainGameSceneDidFinish")]
    private void HandleGameFinished(LevelCompletionResults levelCompletionResults)
    {
        if (levelCompletionResults.levelEndAction != LevelCompletionResults.LevelEndAction.Restart)
        {
            _playViewManager.FinishAll();
        }
    }

    private void InterceptStart(
        MenuTransitionsHelper transitionsHelper,
        IDifficultyBeatmap difficultyBeatmap,
        OverrideEnvironmentSettings? overrideEnvironmentSettings,
        ColorScheme? overrideColorScheme,
        GameplayModifiers gameplayModifiers,
        PlayerSpecificSettings playerSpecificSettings,
        PracticeSettings? practiceSettings,
        string backButtonText,
        bool useTestNoteCutSoundEffects,
        Action? beforeSceneSwitchCallback,
        Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>? levelFinishedCallback,
        LevelSelectionFlowCoordinator flowCoordinator)
    {
        string gameMode = flowCoordinator is PartyFreePlayFlowCoordinator ? "Party" : "Solo";
        var parameters = new StartStandardLevelParameters(
            gameMode,
            difficultyBeatmap,
            overrideEnvironmentSettings,
            overrideColorScheme,
            gameplayModifiers,
            playerSpecificSettings,
            practiceSettings,
            backButtonText,
            useTestNoteCutSoundEffects,
            beforeSceneSwitchCallback,
            levelFinishedCallback);
        _playViewManager.Init(parameters, flowCoordinator);
    }
}
#endif
