using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SongCore.UI;
using SongCore.Utilities;
using UnityEngine;

namespace MappingExtensions.HarmonyPatches
{
    [HarmonyPatch(typeof(SliderMeshController), nameof(SliderMeshController.CutDirectionToControlPointPosition))]
    internal static class SliderMeshControllerCutDirectionToControlPointPositionPatch
    {
        private static void Postfix(ref Vector3 __result, NoteCutDirection noteCutDirection)
        {
            if (!Plugin.active)
            {
                return;
            }

            var direction = (int)noteCutDirection;
            if (direction is >= 1000 and <= 1360 or >= 2000 and <= 2360)
            {
                __result = noteCutDirection.Rotation() * Vector3.down;
            }
        }
    }

    [HarmonyPatch(typeof(NoteCutDirectionExtensions), nameof(NoteCutDirectionExtensions.Direction))]
    internal static class NoteCutDirectionExtensionsDirectionPatch
    {
        private static void Postfix(ref Vector2 __result, NoteCutDirection cutDirection)
        {
            if (!Plugin.active)
            {
                return;
            }

            var direction = (int)cutDirection;
            if (direction is >= 1000 and <= 1360 or >= 2000 and <= 2360)
            {
                var newDirection = cutDirection.Rotation() * Vector3.down;
                __result = new Vector2(newDirection.x, newDirection.y);
            }
        }
    }

    [HarmonyPatch(typeof(NoteCutDirectionExtensions), nameof(NoteCutDirectionExtensions.RotationAngle))]
    internal static class NoteCutDirectionExtensionsRotationAnglePatch
    {
        private static void Postfix(ref float __result, NoteCutDirection cutDirection)
        {
            if (!Plugin.active)
            {
                return;
            }

            var direction = (int)cutDirection;
            if (direction is >= 1000 and <= 1360)
            {
                __result = 1000 - direction;
            }
            else if (direction is >= 2000 and <= 2360)
            {
                __result = 2000 - direction;
            }
        }
    }

    [HarmonyPatch(typeof(ColorNoteVisuals), nameof(ColorNoteVisuals.HandleNoteControllerDidInit))]
    internal static class ColorNoteVisualsHandleNoteControllerDidInitPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // Converts normal notes to arrowless notes when using a certain rotation range.
            return new CodeMatcher(instructions, il)
                .MatchEndForward(
                    new CodeMatch(instruction => instruction.opcode == OpCodes.Bne_Un || instruction.opcode == OpCodes.Bne_Un_S),
                    new CodeMatch(OpCodes.Ldarg_0))
                .ThrowIfInvalid("Could not find ColorNoteVisuals note type branch")
                .CreateLabel(out var destination)
                .MatchStartBackwards(new CodeMatch(instruction => instruction.opcode == OpCodes.Bne_Un || instruction.opcode == OpCodes.Bne_Un_S))
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Beq, destination),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NoteData), nameof(NoteData.cutDirection))),
                    Transpilers.EmitDelegate<Func<NoteCutDirection, bool>>(cutDirection => Plugin.active && (int)cutDirection is >= 2000 and <= 2360))
                .SetOpcodeAndAdvance(OpCodes.Brfalse)
                .InstructionEnumeration();
        }
    }

#if !BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(BeatmapTypeConverters), nameof(BeatmapTypeConverters.ConvertNoteCutDirection))]
    internal static class BeatmapTypeConvertersConvertNoteCutDirectionPatch
    {
        private static void Postfix(ref NoteCutDirection __result, BeatmapSaveDataCommon.NoteCutDirection noteCutDirection)
        {
            var direction = (int)noteCutDirection;
            if (direction is not (>= 1000 and <= 1360 or >= 2000 and <= 2360))
            {
                return;
            }

            // This happens in menu, so we can't rely on Plugin.Active.
            var diffData = RequirementsUI.instance.diffData;
            if (diffData != null && Plugin.HasMappingExtensionsRequirement(diffData.additionalDifficultyData._requirements))
            {
                __result = (NoteCutDirection)direction;
            }
        }
    }

#endif
    [HarmonyPatch(typeof(NoteBasicCutInfoHelper), nameof(NoteBasicCutInfoHelper.GetBasicCutInfo))]
    internal static class NoteBasicCutInfoHelperGetBasicCutInfoPatch
    {
        private static void Prefix(ref NoteCutDirection cutDirection)
        {
            if (!Plugin.active)
            {
                return;
            }

            if ((int)cutDirection is >= 2000 and <= 2360)
            {
                cutDirection = NoteCutDirection.Any;
            }
        }
    }

#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(BeatmapSaveDataVersion3.BeatmapSaveData), nameof(BeatmapSaveDataVersion3.BeatmapSaveData.SpawnRotationForEventValue))]
#else
    [HarmonyPatch(typeof(RotationTimeProcessor), nameof(RotationTimeProcessor.SpawnRotationForEventValue))]
#endif
    internal static class RotationTimeProcessorSpawnRotationForEventValuePatch
    {
#if BEAT_SABER_1_29_0
        private static void Postfix(ref float __result, int __0)
#else
        private static void Postfix(ref int __result, int __0)
#endif
        {
            if (BS_Utils.Plugin.LevelData.IsSet &&
                !Legato.BeatmapKeyExtensions.Is360Degree(Legato.Gameplay.GameplaySceneAccess.Key(BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData)))
            {
                return;
            }

            if (__0 is >= 1000 and <= 1720)
            {
                __result = __0 - 1360;
            }
        }
    }
}
