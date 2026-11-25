#if !PRE_V1_37_1
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
#if !LEGATO_APPLY_PERFORMANCE_PRESET
using System.Runtime.CompilerServices;
#endif
using HarmonyLib;
using IPA.Utilities;

namespace Heck.HarmonyPatches.UntransformedData;

[HeckPatch]
public class HeckGameplayCoreSceneSetupData : GameplayCoreSceneSetupData
{
    private static readonly MethodInfo _heckType = AccessTools.Method(
        typeof(HeckGameplayCoreSceneSetupData),
        nameof(HeckGetType));

    private static readonly MethodInfo _getType = AccessTools.Method(typeof(object), nameof(GetType));

    private static readonly FieldAccessor<GameplayCoreSceneSetupData, BeatmapLevelsModel>.Accessor
        _beatmapLevelsModelAccessor =
            FieldAccessor<GameplayCoreSceneSetupData, BeatmapLevelsModel>.GetAccessor(nameof(_beatmapLevelsModel));

#if !LEGATO_APPLY_PERFORMANCE_PRESET
    // Associate the source with the transformed result before the setup data receives it.
    private static readonly ConditionalWeakTable<IReadonlyBeatmapData, IReadonlyBeatmapData> _untransformedByTransformed = new();
#endif

    private IReadonlyBeatmapData? _untransformedBeatmapData;

    public HeckGameplayCoreSceneSetupData(
        GameplayCoreSceneSetupData original)
        : base(
            original.beatmapKey,
            original.beatmapLevel,
            original.gameplayModifiers,
            original.playerSpecificSettings,
            original.practiceSettings,
#if !LATEST
            original.useTestNoteCutSoundEffects,
#endif
#if !PRE_V1_40_8
            original.targetEnvironmentInfo,
            original.originalEnvironmentInfo,
#else
            original.environmentInfo,
#endif
            original.colorScheme,
#if !PRE_V1_40_8
            original._settingsManager,
#elif LEGATO_APPLY_PERFORMANCE_PRESET
            original._performancePreset,
#endif
            original._audioClipAsyncLoader,
            original._beatmapDataLoader,
            original._beatmapLevelsEntitlementModel,
            original._enableBeatmapDataCaching,
#if LEGATO_APPLY_PERFORMANCE_PRESET
            original._allowNullBeatmapLevelData,
            original.recordingToolData)
#else
            original.environmentsListModel,
            original._allowNullBeatmapLevelData,
            original._beatmapLevelsModel,
            original.beatmapLevelData)
#endif
    {
        GameplayCoreSceneSetupData @this = this;
        _beatmapLevelsModelAccessor(ref @this) = original._beatmapLevelsModel!;
        beatmapLevelData = original.beatmapLevelData;
    }

    public IReadonlyBeatmapData UntransformedBeatmapData =>
        _untransformedBeatmapData ??
        throw new InvalidOperationException($"[{nameof(_untransformedBeatmapData)}] was null.");

#if LEGATO_APPLY_PERFORMANCE_PRESET
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameplayCoreSceneSetupData), nameof(TransformBeatmapData))]
    private static void CaptureForSetupData(GameplayCoreSceneSetupData __instance, IReadonlyBeatmapData beatmapData)
    {
        if (__instance is HeckGameplayCoreSceneSetupData hecked)
        {
            hecked._untransformedBeatmapData = beatmapData;
        }
    }
#else
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BeatmapDataTransformHelper), nameof(BeatmapDataTransformHelper.CreateTransformedBeatmapData))]
    private static void CaptureUntransformed(IReadonlyBeatmapData beatmapData, out IReadonlyBeatmapData __state)
    {
        __state = beatmapData;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BeatmapDataTransformHelper), nameof(BeatmapDataTransformHelper.CreateTransformedBeatmapData))]
    private static void AssociateTransformed(IReadonlyBeatmapData __state, IReadonlyBeatmapData __result)
    {
        if (__state == null || __result == null)
        {
            return;
        }

        _untransformedByTransformed.Remove(__result);
        _untransformedByTransformed.Add(__result, __state);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameplayCoreSceneSetupData), "set_transformedBeatmapData")]
    private static void CaptureForSetupData(GameplayCoreSceneSetupData __instance, IReadonlyBeatmapData value)
    {
        if (__instance is HeckGameplayCoreSceneSetupData hecked && value != null &&
            _untransformedByTransformed.TryGetValue(value, out IReadonlyBeatmapData untransformed))
        {
            hecked._untransformedBeatmapData = untransformed;
        }
    }
#endif

    private static Type HeckGetType(Type original)
    {
        return original == typeof(HeckGameplayCoreSceneSetupData) ? typeof(GameplayCoreSceneSetupData) : original;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ScenesTransitionSetupData), nameof(ScenesTransitionSetupData.InstallBindings))]
    private static IEnumerable<CodeInstruction> HeckOff(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(true, new CodeMatch(instruction => instruction.Calls(_getType)))
            .ThrowIfInvalid("Scene setup type lookup was not found.")
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call, _heckType))
            .InstructionEnumeration();
    }
}
#endif
