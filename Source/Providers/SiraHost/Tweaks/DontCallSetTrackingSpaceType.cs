#if !BEAT_SABER_1_29_0
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.XR;

namespace SiraUtil.Tweaks
{
#if LEGATO_APPLY_PERFORMANCE_PRESET
    [HarmonyPatch(typeof(SettingsApplicatorSO), nameof(SettingsApplicatorSO.ApplyPerformancePreset))]
#else
    [HarmonyPatch(typeof(SettingsApplicatorSO), nameof(SettingsApplicatorSO.ApplyGraphicSettings))]
#endif
    internal class DontCallSetTrackingSpaceType
    {
        // The game still calls this obsolete API; this patch must identify that exact call to remove it.
#pragma warning disable CS0618
        private static readonly MethodInfo XRDeviceSetTrackingSpaceTypeMethod = AccessTools.DeclaredMethod(typeof(XRDevice), nameof(XRDevice.SetTrackingSpaceType));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_1), new CodeMatch(i => i.Calls(XRDeviceSetTrackingSpaceTypeMethod)), new CodeMatch(OpCodes.Pop))
                .ThrowIfInvalid($"Call to {nameof(XRDevice.SetTrackingSpaceType)} not found")
                .RemoveInstructions(3)
                .InstructionEnumeration();
        }
    }
#pragma warning restore CS0618
}

#endif
