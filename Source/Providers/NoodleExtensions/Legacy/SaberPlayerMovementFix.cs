#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck;
using NoodleExtensions.Managers;
using SiraUtil.Affinity;
using UnityEngine;
using Xft;

namespace NoodleExtensions.HarmonyPatches.SmallFixes;

internal sealed class SaberPlayerMovementFix : IAffinity, IDisposable
{
    private static readonly MethodInfo _positionGetter = AccessTools.PropertyGetter(typeof(Transform), nameof(Transform.position));
    private static readonly Bounds _bounds = new(Vector3.zero, Vector3.one * 1_000_000f);

    private readonly PlayerController _player;
    private readonly NoodlePlayerTransformManager _transforms;
    private readonly CodeInstruction _trailPoint;
    private readonly bool _localTrail;

    private SaberPlayerMovementFix(
        PlayerController player,
        NoodlePlayerTransformManager transforms,
        IReadonlyBeatmapData beatmap)
    {
        _player = player;
        _transforms = transforms;
        _localTrail = (beatmap as CustomBeatmapData)?.beatmapCustomData.Get<bool?>(NoodleController.TRAIL_LOCAL_SPACE) ?? false;
        _trailPoint = InstanceTranspilers.EmitInstanceDelegate<Func<Vector3, XWeaponTrail, Vector3>>(TrailPoint);
    }

    public void Dispose() => InstanceTranspilers.DisposeDelegate(_trailPoint);

    [AffinityPrefix]
    [AffinityPatch(typeof(SaberMovementData), nameof(SaberMovementData.AddNewData))]
    private void RecordBladeMotion(SaberMovementData __instance, ref Vector3 topPos, ref Vector3 bottomPos)
    {
        if (!UsesLocalHistory(__instance))
        {
            return;
        }

        Transform root = _transforms.Root;
        topPos = root.InverseTransformPoint(topPos);
        bottomPos = root.InverseTransformPoint(bottomPos);
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(SaberMovementData), nameof(SaberMovementData.lastAddedData), AffinityMethodType.Getter)]
    private void ReadBladeMotion(SaberMovementData __instance, ref SaberMovementData.Data __result)
    {
        if (UsesLocalHistory(__instance) && __instance._validCount > 0)
        {
            RestoreWorld(ref __result);
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(SaberMovementData), nameof(SaberMovementData.ComputeCutPlaneNormal))]
    private void ReadCutPlane(SaberMovementData __instance, ref Vector3 __result)
    {
        if (UsesLocalHistory(__instance))
        {
            __result = _transforms.Root.TransformDirection(__result);
        }
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(SaberSwingRatingCounter), nameof(SaberSwingRatingCounter.ProcessNewData))]
    private void ReadRatingSamples(SaberMovementData ____saberMovementData, ref SaberMovementData.Data newData, ref SaberMovementData.Data prevData)
    {
        if (UsesLocalHistory(____saberMovementData))
        {
            RestoreWorld(ref newData);
            RestoreWorld(ref prevData);
        }
    }

    [AffinityTranspiler]
    [AffinityPatch(typeof(XWeaponTrail), "LateUpdate")]
    [AffinityPatch(typeof(XWeaponTrail), nameof(XWeaponTrail.curHeadPos), AffinityMethodType.Getter)]
    [AffinityPatch(typeof(XWeaponTrail), "UpdateHeadElem")]
    [AffinityPatch(typeof(XWeaponTrail), "RecordCurElem")]
    private IEnumerable<CodeInstruction> SampleTrailPoints(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (instruction.opcode == OpCodes.Callvirt && Equals(instruction.operand, _positionGetter))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(_trailPoint.opcode, _trailPoint.operand);
            }
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(XWeaponTrail), "Start")]
    private void ParentTrailRenderer(XWeaponTrail __instance, XWeaponTrailRenderer ____trailRenderer)
    {
        if (_localTrail && IsPlayerTrail(__instance))
        {
            ____trailRenderer.transform.SetParent(_transforms.Root, false);
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(XWeaponTrail), "LateUpdate")]
    private void KeepTrailBounds(XWeaponTrail __instance, XWeaponTrailRenderer ____trailRenderer)
    {
        if (____trailRenderer != null && IsPlayerTrail(__instance))
        {
            ____trailRenderer.mesh.bounds = _bounds;
        }
    }

    private bool IsPlayerTrail(XWeaponTrail trail)
    {
        return _transforms.Active &&
               (trail.transform.IsChildOf(_transforms.LeftHand) || trail.transform.IsChildOf(_transforms.RightHand));
    }

    private bool UsesLocalHistory(SaberMovementData movement)
    {
        return _transforms.Active && !_localTrail &&
               (ReferenceEquals(_player.leftSaber._movementData, movement) || ReferenceEquals(_player.rightSaber._movementData, movement));
    }

    private void RestoreWorld(ref SaberMovementData.Data sample)
    {
        Transform root = _transforms.Root;
        sample.topPos = root.TransformPoint(sample.topPos);
        sample.bottomPos = root.TransformPoint(sample.bottomPos);
        sample.segmentNormal = root.TransformDirection(sample.segmentNormal);
    }

    private Vector3 TrailPoint(Vector3 worldPoint, XWeaponTrail trail)
    {
        return _localTrail && IsPlayerTrail(trail) ? _transforms.Root.InverseTransformPoint(worldPoint) : worldPoint;
    }
}
#endif
