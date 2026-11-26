#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck;
using NoodleExtensions.Animation;
using SiraUtil.Affinity;
using UnityEngine;

namespace NoodleExtensions.HarmonyPatches.Objects;

internal sealed class NoteJumpNoodlifier : IAffinity, IDisposable
{
    private static readonly MethodInfo _songTimeGetter =
        AccessTools.PropertyGetter(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.songTime));

    private static readonly MethodInfo _rotationSetter =
        AccessTools.PropertySetter(typeof(Transform), nameof(Transform.localRotation));

    private static readonly MethodInfo _positionSetter =
        AccessTools.PropertySetter(typeof(Transform), nameof(Transform.position));

    private static readonly MethodInfo _localPositionSetter =
        AccessTools.PropertySetter(typeof(Transform), nameof(Transform.localPosition));

    private static readonly FieldInfo _worldRotation =
        AccessTools.Field(typeof(NoteJump), nameof(NoteJump._worldRotation));

    private static readonly FieldInfo _localPosition =
        AccessTools.Field(typeof(NoteJump), nameof(NoteJump._localPosition));

    private readonly NoteUpdateNoodlifier _noteUpdate;
    private readonly AnimationHelper _animation;
    private readonly CodeInstruction _adjustClock;
    private readonly CodeInstruction _adjustLook;
    private readonly CodeInstruction _setPosition;

    private NoteJumpNoodlifier(NoteUpdateNoodlifier noteUpdate, AnimationHelper animation)
    {
        _noteUpdate = noteUpdate;
        _animation = animation;
        _adjustClock = InstanceTranspilers.EmitInstanceDelegate<Func<float, NoteJump, float>>(AdjustClock);
        _adjustLook = InstanceTranspilers.EmitInstanceDelegate<Func<Quaternion, NoteJump, Quaternion>>(AdjustLook);
        _setPosition = InstanceTranspilers.EmitInstanceDelegate<Action<NoteJump, float>>(SetDefinitePosition);
    }

    public void Dispose()
    {
        InstanceTranspilers.DisposeDelegate(_adjustClock);
        InstanceTranspilers.DisposeDelegate(_adjustLook);
        InstanceTranspilers.DisposeDelegate(_setPosition);
    }

    [AffinityTranspiler]
    [AffinityPatch(typeof(NoteJump), nameof(NoteJump.ManualUpdate))]
    private IEnumerable<CodeInstruction> ApplyAnimation(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _songTimeGetter))
            .ThrowIfInvalid("Could not find the native note jump clock")
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0), _adjustClock)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _rotationSetter))
            .ThrowIfInvalid("Could not find the native note look rotation")
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0), _adjustLook)
            .MatchForward(
                false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, _worldRotation),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, _localPosition))
            .ThrowIfInvalid("Could not find the native note jump position");

        // Finishing a note can branch directly to the position calculation.
        var loadJump = new CodeInstruction(OpCodes.Ldarg_0);
        loadJump.labels.AddRange(matcher.Labels);
        matcher.Labels.Clear();
        return matcher
            .InsertAndAdvance(loadJump, new CodeInstruction(OpCodes.Ldloc_1), _setPosition)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _positionSetter))
            .ThrowIfInvalid("Could not find the native note jump position setter")
            .SetOperandAndAdvance(_localPositionSetter)
            .InstructionEnumeration();
    }

    private float AdjustClock(float songTime, NoteJump jump)
    {
        float? time = _noteUpdate.NoodleData?.GetTimeProperty();
        return time.HasValue ? jump._beatTime + ((time.Value - 0.5f) * jump._jumpDuration) : songTime;
    }

    private Quaternion AdjustLook(Quaternion rotation, NoteJump jump)
    {
        return _noteUpdate.NoodleData is { DisableLook: true } ? jump._endRotation : rotation;
    }

    private void SetDefinitePosition(NoteJump jump, float time)
    {
        if (_noteUpdate.NoodleData is not NoodleObjectData data)
        {
            return;
        }

        _animation.GetDefinitePositionOffset(data.AnimationObject, data.Track, time, out Vector3? position);
        if (position.HasValue)
        {
            jump._localPosition = position.Value + data.InternalNoteOffset;
        }
    }
}
#endif
