#if LEGATO_EARLY_STANDARD_MAPS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck.PlayView;
using SiraUtil.Affinity;

namespace Heck.HarmonyPatches;

internal sealed class PlayViewInterrupter : IAffinity, IDisposable
{
    private static readonly MethodInfo _start = AccessTools.Method(typeof(MenuSceneSetupData), nameof(MenuSceneSetupData.StartLevel));
    private static readonly AccessTools.FieldRef<StandardLevelSelectionFlowCoordinator, StandardLevelDetailViewController> _detail =
        AccessTools.FieldRefAccess<StandardLevelSelectionFlowCoordinator, StandardLevelDetailViewController>("_levelDetailViewController");

    private readonly PlayViewManager _playViews;
    private readonly CodeInstruction _redirect;

    private PlayViewInterrupter(PlayViewManager playViews)
    {
        _playViews = playViews;
        _redirect = InstanceTranspilers.EmitInstanceDelegate<Action<MenuSceneSetupData, IStandardLevelDifficultyBeatmap,
            GameplayOptions, GameplayMode, StandardLevelSelectionFlowCoordinator>>(StartWithPlayViews);
    }

    public void Dispose() => InstanceTranspilers.DisposeDelegate(_redirect);

    [AffinityTranspiler]
    [AffinityPatch(typeof(StandardLevelSelectionFlowCoordinator), "StartLevel")]
    private IEnumerable<CodeInstruction> RedirectStart(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (!instruction.Calls(_start))
            {
                yield return instruction;
                continue;
            }

            CodeInstruction loadCoordinator = new(OpCodes.Ldarg_0);
            loadCoordinator.labels.AddRange(instruction.labels);
            loadCoordinator.blocks.AddRange(instruction.blocks);
            yield return loadCoordinator;
            yield return new CodeInstruction(_redirect.opcode, _redirect.operand);
        }
    }

    private void StartWithPlayViews(
        MenuSceneSetupData menu,
        IStandardLevelDifficultyBeatmap difficulty,
        GameplayOptions options,
        GameplayMode mode,
        StandardLevelSelectionFlowCoordinator coordinator) =>
        _playViews.Begin(new StartStandardLevelParameters(menu, _detail(coordinator), difficulty, options, mode));
}
#endif
