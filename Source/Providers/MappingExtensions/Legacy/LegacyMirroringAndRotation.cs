using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MappingExtensions.HarmonyPatches
{
    [HarmonyPatch]
    internal static class LegacyMappingLevelStart
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(StandardLevelScenesTransitionSetupDataSO), nameof(StandardLevelScenesTransitionSetupDataSO.Init));
            yield return AccessTools.Method(typeof(MissionLevelScenesTransitionSetupDataSO), nameof(MissionLevelScenesTransitionSetupDataSO.Init));
        }

        private static void Prefix(IDifficultyBeatmap difficultyBeatmap) => Plugin.PrepareLegacyLevel(difficultyBeatmap);
    }

    [HarmonyPatch(typeof(BeatmapObjectData), nameof(BeatmapObjectData.MirrorLineIndex))]
    internal static class LegacyMirrorObjectLane
    {
        private static void Prefix(BeatmapObjectData __instance, out int __state) => __state = __instance.lineIndex;

        private static void Postfix(BeatmapObjectData __instance, int lineCount, int __state)
        {
            if (MappingExtensionsData.IsPrecisionValue(__state))
                __instance.lineIndex = LegacyPlacement.Encode(lineCount - 1f - LegacyPlacement.Decode(__state));
        }
    }

    [HarmonyPatch(typeof(NoteData), nameof(NoteData.MirrorLineIndex))]
    internal static class LegacyMirrorNoteLanes
    {
        private static void Prefix(NoteData __instance, out (int Line, int Flip) __state) =>
            __state = (__instance.lineIndex, __instance.flipLineIndex);

        private static void Postfix(NoteData __instance, int lineCount, (int Line, int Flip) __state)
        {
            if (MappingExtensionsData.IsPrecisionValue(__state.Line))
                __instance.lineIndex = LegacyPlacement.Encode(lineCount - 1f - LegacyPlacement.Decode(__state.Line));
            if (MappingExtensionsData.IsPrecisionValue(__state.Flip))
                __instance.flipLineIndex = LegacyPlacement.Encode(lineCount - 1f - LegacyPlacement.Decode(__state.Flip));
        }
    }

    [HarmonyPatch(typeof(ObstacleData), nameof(ObstacleData.MirrorLineIndex))]
    internal static class LegacyMirrorWallLane
    {
        private static void Prefix(ObstacleData __instance, out int __state) => __state = __instance.lineIndex;

        private static void Postfix(ObstacleData __instance, int lineCount, int __state)
        {
            bool precisionWidth = MappingExtensionsData.TryDecodePrecisionWidth(__instance.width, out float width);
            if (precisionWidth || MappingExtensionsData.IsPrecisionValue(__state))
                __instance.lineIndex = LegacyPlacement.Encode(lineCount - width - LegacyPlacement.Decode(__state));
        }
    }

    internal static class LegacyNoteAngles
    {
        internal static bool TryDecode(NoteCutDirection direction, out int origin, out float angle)
        {
            int value = (int)direction;
            origin = value >= 2000 ? 2000 : 1000;
            angle = origin - value;
            return value >= origin && value <= origin + 360;
        }
    }

    [HarmonyPatch(typeof(NoteData), nameof(NoteData.MirrorTransformCutDirection))]
    internal static class LegacyMirrorNoteAngle
    {
        private static void Postfix(NoteData __instance)
        {
            if (LegacyNoteAngles.TryDecode(__instance.cutDirection, out int origin, out float angle))
                __instance.cutDirection = (NoteCutDirection)(origin + 360 + (int)angle);
        }
    }

    [HarmonyPatch(typeof(NoteCutDirectionExtensions), nameof(NoteCutDirectionExtensions.RotationAngle))]
    internal static class LegacyNoteAngle
    {
        private static void Postfix(NoteCutDirection cutDirection, ref float __result)
        {
            if (LegacyNoteAngles.TryDecode(cutDirection, out _, out float angle)) __result = angle;
        }
    }

    [HarmonyPatch(typeof(NoteCutDirectionExtensions), nameof(NoteCutDirectionExtensions.Rotation))]
    internal static class LegacyNoteRotation
    {
        private static void Postfix(NoteCutDirection cutDirection, ref Quaternion __result)
        {
            if (LegacyNoteAngles.TryDecode(cutDirection, out _, out float angle)) __result = Quaternion.Euler(0f, 0f, angle);
        }
    }

    [HarmonyPatch(typeof(NoteCutDirectionExtensions), nameof(NoteCutDirectionExtensions.Direction))]
    internal static class LegacyNoteDirection
    {
        private static void Postfix(NoteCutDirection cutDirection, ref Vector2 __result)
        {
            if (LegacyNoteAngles.TryDecode(cutDirection, out _, out float angle))
                __result = Quaternion.Euler(0f, 0f, angle) * Vector3.down;
        }
    }

    [HarmonyPatch(typeof(SpawnRotationProcessor), nameof(SpawnRotationProcessor.RotationForEventValue))]
    internal static class LegacySpawnRotation
    {
        internal static bool IsPrecisionRotation(int value) => value is >= 1000 and <= 1720;

        private static void Postfix(int index, ref float __result)
        {
            if (Plugin.Requires360Movement && IsPrecisionRotation(index)) __result = index - 1360;
        }
    }

    [HarmonyPatch(typeof(BeatDataMirrorTransform), nameof(BeatDataMirrorTransform.CreateTransformedData))]
    internal static class LegacyMirrorRotationEvents
    {
        private static int MirrorRotation(int vanillaMaximum, int value) =>
            LegacySpawnRotation.IsPrecisionRotation(value) ? 2720 - value : vanillaMaximum - value;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var value = AccessTools.Field(typeof(BeatmapEventData), nameof(BeatmapEventData.value));
            var mirror = AccessTools.Method(typeof(LegacyMirrorRotationEvents), nameof(MirrorRotation));
            var subtraction = Enumerable.Range(1, codes.Count - 1)
                .Single(index => codes[index].opcode == OpCodes.Sub && codes[index - 1].LoadsField(value));
            codes[subtraction].opcode = OpCodes.Call;
            codes[subtraction].operand = mirror;
            return codes;
        }
    }
}
