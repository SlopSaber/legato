#if LEGATO_SIRA_EARLY_CONTEXTS
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Legato.Sira
{
    [HarmonyPatch(typeof(SaberBurnMarkArea), "Update")]
    internal static class EarlyBurnMarkPatch
    {
        internal static bool SupportsExtraSabers { get; private set; }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UseSaberCount(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);
            FieldInfo saberField = AccessTools.DeclaredField(typeof(SaberBurnMarkArea), "_sabers");
            FieldInfo linesField = AccessTools.DeclaredField(typeof(SaberBurnMarkArea), "_lineRenderers");
            FieldInfo cameraField = AccessTools.DeclaredField(typeof(SaberBurnMarkArea), "_camera");
            MethodInfo render = AccessTools.DeclaredMethod(typeof(Camera), nameof(Camera.Render));
            MethodInfo enabledGetter = AccessTools.PropertyGetter(typeof(Renderer), nameof(Renderer.enabled));
            int limit = -1;
            int renderCheck = -1;
            for (int index = 1; index + 1 < code.Count; index++)
            {
                if (code[index - 1].opcode == OpCodes.Ldloc_0 && code[index].opcode == OpCodes.Ldc_I4_2
                    && (code[index + 1].opcode == OpCodes.Blt || code[index + 1].opcode == OpCodes.Blt_S))
                {
                    if (limit >= 0)
                    {
                        return Unsupported(code);
                    }

                    limit = index;
                }

                if (index + 15 < code.Count && IsEnabledCheck(code, index, linesField, enabledGetter, OpCodes.Ldc_I4_0)
                    && BranchesTo(code[index + 5], OpCodes.Brtrue, OpCodes.Brtrue_S, code[index + 12])
                    && IsEnabledCheck(code, index + 6, linesField, enabledGetter, OpCodes.Ldc_I4_1)
                    && BranchesTo(code[index + 11], OpCodes.Brfalse, OpCodes.Brfalse_S, code[index + 15])
                    && code[index + 12].opcode == OpCodes.Ldarg_0
                    && code[index + 13].opcode == OpCodes.Ldfld && Equals(code[index + 13].operand, cameraField)
                    && code[index + 14].opcode == OpCodes.Callvirt && Equals(code[index + 14].operand, render))
                {
                    if (renderCheck >= 0)
                    {
                        return Unsupported(code);
                    }

                    renderCheck = index;
                }
            }

            if (limit < 0 || renderCheck <= limit)
            {
                return Unsupported(code);
            }

            for (int index = renderCheck + 2; index <= renderCheck + 11; index++)
            {
                if (code[index].labels.Count != 0 || code[index].blocks.Count != 0)
                {
                    return Unsupported(code);
                }
            }

            // Keep the native camera render, texture fade, and sprite timing.
            CodeInstruction skipRender = code[renderCheck + 11];
            code.RemoveRange(renderCheck + 2, 10);
            code.InsertRange(renderCheck + 2, new[]
            {
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EarlyBurnMarkPatch), nameof(HasEnabledLine))),
                skipRender,
            });

            code[limit].opcode = OpCodes.Ldarg_0;
            code[limit].operand = null;
            code.InsertRange(limit + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldfld, saberField),
                new CodeInstruction(OpCodes.Ldlen),
                new CodeInstruction(OpCodes.Conv_I4),
            });
            SupportsExtraSabers = true;
            return code;
        }

        private static bool IsEnabledCheck(List<CodeInstruction> code, int index, FieldInfo field, MethodInfo getter, OpCode slot)
        {
            return code[index].opcode == OpCodes.Ldarg_0
                && code[index + 1].opcode == OpCodes.Ldfld && Equals(code[index + 1].operand, field)
                && code[index + 2].opcode == slot
                && code[index + 3].opcode == OpCodes.Ldelem_Ref
                && code[index + 4].opcode == OpCodes.Callvirt && Equals(code[index + 4].operand, getter);
        }

        private static bool BranchesTo(CodeInstruction branch, OpCode longForm, OpCode shortForm, CodeInstruction destination)
        {
            return (branch.opcode == longForm || branch.opcode == shortForm)
                && branch.operand is Label target && destination.labels.Contains(target);
        }

        private static IEnumerable<CodeInstruction> Unsupported(List<CodeInstruction> code)
        {
            SupportsExtraSabers = false;
            Debug.LogWarning("SiraUtil could not find the native floor burn loop. Stock sabers retain floor burn effects.");
            return code;
        }

        private static bool HasEnabledLine(LineRenderer[] lines)
        {
            foreach (LineRenderer line in lines)
            {
                if (line != null && line.enabled)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
