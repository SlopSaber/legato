#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using NoodleExtensions.Managers;
using SiraUtil.Affinity;
using UnityEngine;

namespace NoodleExtensions.HarmonyPatches.SmallFixes;

internal sealed class PlayerTransformsNoodlePatch : IAffinity
{
    private readonly NoodlePlayerTransformManager _transforms;

    private PlayerTransformsNoodlePatch(NoodlePlayerTransformManager transforms)
    {
        _transforms = transforms;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(PlayerController), nameof(PlayerController.MoveTowardsHead))]
    private bool MoveTowardsTrackedHead(
        float start,
        float end,
        Quaternion noteInverseWorldRotation,
        float t,
        Vector3 ____headPos,
        bool ____overrideHeadPos,
        ref float __result)
    {
        if (!_transforms.Active || ____overrideHeadPos)
        {
            return true;
        }

        Transform head = _transforms.Head;
        Vector3 trackingOffset = head.InverseTransformPoint(____headPos);
        float z = (noteInverseWorldRotation * trackingOffset).z;
        __result = Mathf.LerpUnclamped(start + (z * Mathf.Min(1f, t * 2f)), end + z, t);
        return false;
    }
}
#endif
