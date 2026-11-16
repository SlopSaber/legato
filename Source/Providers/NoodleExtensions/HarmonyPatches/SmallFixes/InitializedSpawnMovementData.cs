using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Heck;
using UnityEngine;

namespace NoodleExtensions.HarmonyPatches.SmallFixes;

[HeckPatch(PatchType.Features)]
[HarmonyPatch(typeof(BeatmapObjectSpawnController))]
internal class InitializedSpawnMovementData
{
    // Initializes movement data during construction so dependent services can use it before Start.
    private InitializedSpawnMovementData(
        BeatmapObjectSpawnController.InitData initData,
        IJumpOffsetYProvider jumpOffsetYProvider,
#if !PRE_V1_40_8
        BeatmapObjectSpawnController spawnController,
        IReadonlyBeatmapData beatmapData,
        IVariableMovementDataProvider variableMovementDataProvider)
    {
        BeatmapObjectSpawnMovementData beatmapObjectSpawnMovementData = spawnController.beatmapObjectSpawnMovementData;
        beatmapObjectSpawnMovementData.Init(
            initData.noteLinesCount,
            jumpOffsetYProvider,
            Vector3.right);

#if BEAT_SABER_1460
        float minRelativeNoteJumpSpeed = BeatmapObjectSpawnControllerHelpers.GetMinRelativeNoteJumpSpeedUntilLastBeatmapObject(beatmapData);
#else
        float minRelativeNoteJumpSpeed = beatmapData
            .GetBeatmapDataItems<NoteJumpSpeedEventData>(0)
            .Aggregate(0.0f, (current, beatmapDataItem) => Mathf.Min(current, beatmapDataItem.relativeNoteJumpSpeed));
#endif
        variableMovementDataProvider.Init(
            beatmapObjectSpawnMovementData.startHalfJumpDurationInBeats,
            beatmapObjectSpawnMovementData.maxHalfJumpDistance,
            initData.noteJumpMovementSpeed,
            minRelativeNoteJumpSpeed,
            initData.beatsPerMinute,
            initData.noteJumpValueType,
            initData.noteJumpValue,
            beatmapObjectSpawnMovementData.centerPos,
            Vector3.forward);
#else
        IBeatmapObjectSpawnController spawnController)
    {
        spawnController.beatmapObjectSpawnMovementData.Init(
            initData.noteLinesCount,
            initData.noteJumpMovementSpeed,
            initData.beatsPerMinute,
            initData.noteJumpValueType,
            initData.noteJumpValue,
            jumpOffsetYProvider,
            Vector3.right,
            Vector3.forward);
#endif
        MovementData = spawnController.beatmapObjectSpawnMovementData;
    }

    internal BeatmapObjectSpawnMovementData MovementData { get; }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(BeatmapObjectSpawnController.Start))]
    private static IEnumerable<CodeInstruction> RemoveInitTranspiler(IEnumerable<CodeInstruction> instructions)
    {
#if !PRE_V1_40_8
        var initMethod = AccessTools.Method(typeof(IVariableMovementDataProvider), nameof(IVariableMovementDataProvider.Init));
#else
        var initMethod = AccessTools.Method(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.Init));
#endif
        var matcher = new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(instruction => instruction.Calls(initMethod)))
            .ThrowIfInvalid("Movement initialization boundary was not found.");
        int count = matcher.Pos + 1;
        return matcher.Start().RemoveInstructions(count).InstructionEnumeration();
    }
}
