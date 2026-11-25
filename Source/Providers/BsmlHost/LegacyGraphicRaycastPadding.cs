#if BEAT_SABER_1_29_0 || LEGATO_HMUI_LEGACY_GRAPHICS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using VRUIControls;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    /// <summary>
    /// Applies BSML's raycast padding to the legacy UI raycasters.
    /// </summary>
    [HarmonyPatch]
    internal static class LegacyGraphicRaycastPadding
    {
        private static readonly MethodInfo NativeContains = AccessTools.Method(typeof(RectTransformUtility), nameof(RectTransformUtility.RectangleContainsScreenPoint), new[] { typeof(RectTransform), typeof(Vector2), typeof(Camera) });
        private static readonly MethodInfo PaddedContains = AccessTools.Method(typeof(Legato.Unity.RaycastPadding), nameof(Legato.Unity.RaycastPadding.Contains));

        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in new[] { typeof(GraphicRaycaster), typeof(VRGraphicRaycaster) })
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.Name == "Raycast" && method.GetMethodBody() != null)
                    {
                        yield return method;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(NativeContains))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = PaddedContains;
                }

                yield return instruction;
            }
        }
    }
}
#endif
