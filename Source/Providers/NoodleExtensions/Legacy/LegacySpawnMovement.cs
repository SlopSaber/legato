#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck;
using Heck.Deserialize;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;

namespace NoodleExtensions.HarmonyPatches.ObjectProcessing;

internal sealed class LegacySpawnMovement : IAffinity, IDisposable
{
    private static readonly MethodInfo _jumpDurationGetter =
        AccessTools.PropertyGetter(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.jumpDuration));

    private readonly DeserializedData _data;
    private readonly CodeInstruction _resolveDuration;

    private LegacySpawnMovement([Inject(Id = NoodleController.ID)] DeserializedData data)
    {
        _data = data;
        _resolveDuration =
            InstanceTranspilers.EmitInstanceDelegate<Func<float, BeatmapObjectSpawnController, BeatmapObjectData, float>>(ResolveDuration);
    }

    public void Dispose() => InstanceTranspilers.DisposeDelegate(_resolveDuration);

    internal static float JumpDuration(BeatmapObjectSpawnMovementData movement, NoodleObjectData? data, float bpm)
    {
        if (bpm <= 0f)
        {
            return movement.jumpDuration;
        }

        float speed = data?.Njs ?? (movement._startNoteJumpMovementSpeed * bpm / movement._startBPM);
        float beatDuration = 60f / bpm;
        float halfJump = movement._startHalfJumpDurationInBeats;
        while (speed * beatDuration * halfJump > movement._maxHalfJumpDistance && halfJump > 0.001f)
        {
            halfJump *= 0.5f;
        }

        return 2f * beatDuration * Mathf.Max(1f, halfJump + (data?.SpawnOffset ?? movement._noteJumpStartBeatOffset));
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(BeatmapObjectSpawnController), "SpawnNote")]
    private void ApplyCutDirection(NoteData noteData, ref float cutDirectionAngleOffset)
    {
        if (_data.Resolve(noteData, out NoodleNoteData? data) && data.CutDirectionOffset is float angle)
        {
            cutDirectionAngleOffset = angle;
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.GetNoteSpawnMovementData))]
    private void ApplyNoteMovement(
        BeatmapObjectSpawnMovementData __instance,
        NoteData noteData,
        ref Vector3 moveStartPos,
        ref Vector3 moveEndPos,
        ref Vector3 jumpEndPos,
        ref float jumpGravity)
    {
        if (!_data.Resolve(noteData, out NoodleBaseNoteData? data))
        {
            return;
        }

        float jumpDuration = JumpDuration(__instance, data, EffectiveBpm(__instance));
        float lineIndex = data.StartX + (__instance._noteLinesCount * 0.5f) ?? noteData.lineIndex;
        float flipIndex = data.InternalFlipLineIndex ??
                          (noteData.flipLineIndex == noteData.lineIndex ? lineIndex : noteData.flipLineIndex);
        float startLayer = data.InternalStartNoteLineLayer;
        float y = data.StartY is float height
            ? __instance._baseLinesYPos + (height * __instance.noteLinesDistance)
            : __instance.LineYPosForLineLayer(noteData.noteLineLayer);
        float startY = __instance._baseLinesYPos + (startLayer * __instance.noteLinesDistance);
        float apex = data.StartY.HasValue
            ? (0.875f * y) + 0.639583f + __instance._jumpOffsetY
            : __instance.HighestJumpPosYForLineLayer(noteData.noteLineLayer);
        float launchY = data.DisableGravity ? y : startY;
        float halfJump = jumpDuration * 0.5f;
        jumpGravity = halfJump > 0f ? 2f * (apex - launchY) / (halfJump * halfJump) : jumpGravity;

        GetPositions(__instance, data, jumpDuration, out moveStartPos, out moveEndPos, out jumpEndPos);
        Vector3 startOffset = LaneOffset(__instance, flipIndex, launchY);
        moveStartPos += startOffset;
        moveEndPos += startOffset;
        jumpEndPos += LaneOffset(__instance, lineIndex, startY);
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.GetObstacleSpawnMovementData))]
    private void ApplyObstacleMovement(
        BeatmapObjectSpawnMovementData __instance,
        ObstacleData obstacleData,
        ref Vector3 moveStartPos,
        ref Vector3 moveEndPos,
        ref Vector3 jumpEndPos,
        ref float obstacleHeight)
    {
        if (!_data.Resolve(obstacleData, out NoodleObstacleData? data))
        {
            return;
        }

        float index = data.StartX + (__instance._noteLinesCount * 0.5f) ?? obstacleData.lineIndex;
        float y = data.StartY is float height
            ? __instance._verticalObstaclePosY + (height * __instance.noteLinesDistance)
            : obstacleData.obstacleType == ObstacleType.Top
                ? __instance._topObstaclePosY + __instance._jumpOffsetY
                : __instance._verticalObstaclePosY;
        GetPositions(
            __instance,
            data,
            JumpDuration(__instance, data, EffectiveBpm(__instance)),
            out moveStartPos,
            out moveEndPos,
            out jumpEndPos);
        Vector3 offset = LaneOffset(__instance, index, y);
        moveStartPos += offset;
        moveEndPos += offset;
        jumpEndPos += offset;
        obstacleHeight = data.Height * __instance.noteLinesDistance ?? obstacleHeight;
    }

    [AffinityTranspiler]
    [AffinityPatch(typeof(BeatmapObjectSpawnController), "SpawnNote")]
    [AffinityPatch(typeof(BeatmapObjectSpawnController), "SpawnObstacle")]
    private IEnumerable<CodeInstruction> ApplyDuration(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _jumpDurationGetter))
            .ThrowIfInvalid("Could not find the native jump duration")
            .Advance(1)
            .Insert(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1), _resolveDuration)
            .InstructionEnumeration();
    }

    private static float EffectiveBpm(BeatmapObjectSpawnMovementData movement)
    {
        return movement._startNoteJumpMovementSpeed > 0f
            ? movement._startBPM * movement._noteJumpMovementSpeed / movement._startNoteJumpMovementSpeed
            : movement._startBPM;
    }

    private static Vector3 LaneOffset(BeatmapObjectSpawnMovementData movement, float lineIndex, float y)
    {
        float x = (lineIndex - ((movement._noteLinesCount - 1f) * 0.5f)) * movement.noteLinesDistance;
        return (movement._rightVec * x) + (Vector3.up * y);
    }

    private static void GetPositions(
        BeatmapObjectSpawnMovementData movement,
        NoodleObjectData data,
        float duration,
        out Vector3 start,
        out Vector3 mid,
        out Vector3 end)
    {
        float distance = (data.Njs ?? movement._noteJumpMovementSpeed) * duration * 0.5f;
        Vector3 travel = movement._forwardVec * distance;
        mid = movement._centerPos + travel;
        end = movement._centerPos - travel;
        start = mid + (movement._forwardVec * movement._moveDistance);
    }

    private float ResolveDuration(float original, BeatmapObjectSpawnController spawner, BeatmapObjectData item)
    {
        return _data.Resolve(item, out NoodleObjectData? data)
            ? JumpDuration(spawner._beatmapObjectSpawnMovementData, data, spawner.currentBPM)
            : original;
    }
}
#endif
