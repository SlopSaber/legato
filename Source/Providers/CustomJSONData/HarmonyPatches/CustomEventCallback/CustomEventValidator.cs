using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapCallbacksController))]
    internal static class CustomEventValidator
    {
        private static readonly MemberInfo _itemType =
            (MemberInfo?)AccessTools.PropertyGetter(typeof(BeatmapDataItem), nameof(BeatmapDataItem.type)) ??
            AccessTools.Field(typeof(BeatmapDataItem), nameof(BeatmapDataItem.type));

        private static readonly MethodInfo _isEvent = AccessTools.Method(typeof(CustomEventValidator), nameof(IsEvent));

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(BeatmapCallbacksController.ManualUpdate))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(instruction => Equals(instruction.operand, _itemType)),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(instruction => instruction.opcode == OpCodes.Beq || instruction.opcode == OpCodes.Beq_S))
                .ThrowIfInvalid("Beatmap callback event comparison was not found.")
                .Advance(1)
                .SetAndAdvance(OpCodes.Call, _isEvent)
                .SetOpcodeAndAdvance(OpCodes.Brtrue)
                .InstructionEnumeration();
        }

        private static bool IsEvent(BeatmapDataItem.BeatmapDataItemType type)
        {
            return type == BeatmapDataItem.BeatmapDataItemType.BeatmapEvent || type == (BeatmapDataItem.BeatmapDataItemType)2;
        }
    }
}
