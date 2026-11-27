using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MappingExtensions.HarmonyPatches
{
    internal static class LegacyPlacement
    {
        internal static float Decode(int index) => MappingExtensionsData.IsPrecisionValue(index)
            ? (MappingExtensionsData.NormalizePrecisionLineIndex(index) - 1000f) / 1000f
            : index;

        internal static int Encode(float coordinate)
        {
            int units = Mathf.RoundToInt(coordinate * 1000f);
            return units < 0 ? units - 1000 : units + 1000;
        }

        internal static int StorageIndex(int lineIndex) => Mathf.Clamp(Mathf.FloorToInt(Decode(lineIndex)), 0, 3);

        internal static IEnumerable<CodeInstruction> ConvertLineIndexReads(IEnumerable<CodeInstruction> instructions,
            string conversion, int expectedReads)
        {
            var getter = AccessTools.PropertyGetter(typeof(BeatmapObjectData), nameof(BeatmapObjectData.lineIndex));
            var convert = AccessTools.Method(typeof(LegacyPlacement), conversion);
            int reads = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!instruction.Calls(getter)) continue;
                yield return new CodeInstruction(OpCodes.Call, convert);
                reads++;
            }
            if (reads != expectedReads)
                throw new System.InvalidOperationException($"Expected {expectedReads} native line index reads; found {reads}.");
        }

        internal static bool TryDecodeLayer(int layer, float baseHeight, float spacing, out float height)
        {
            height = baseHeight + Decode(layer) * spacing;
            return MappingExtensionsData.IsPrecisionValue(layer) || (Plugin.active && layer is < 0 or > 2);
        }
    }

    [HarmonyPatch(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.GetBeatmapDataFromBeatmapSaveData))]
    internal static class LegacyBeatmapLoaderLanes
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // Preview and gameplay loads can use this loader concurrently.
            var processorType = typeof(BeatmapDataLoader.NotesInTimeRowProcessor);
            var processor = generator.DeclareLocal(processorType);
            var processorField = AccessTools.DeclaredField(typeof(BeatmapDataLoader), "_notesInTimeRowProcessor");
            var codes = instructions.ToList();
            int processorReads = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldfld || !Equals(codes[i].operand, processorField)) continue;
                codes[i].opcode = OpCodes.Pop;
                codes[i].operand = null;
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldloc, processor));
                processorReads++;
            }
            if (processorReads != 4)
                throw new System.InvalidOperationException($"Expected four native row processor reads; found {processorReads}.");
            codes.Insert(0, new CodeInstruction(OpCodes.Stloc, processor));
            codes.Insert(0, new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(processorType)));

            // Lane buckets are fixed at four. The original encoded index stays on each object.
            var add = AccessTools.Method(typeof(List<BeatmapObjectData>), nameof(List<BeatmapObjectData>.Add));
            var storageIndex = AccessTools.Method(typeof(LegacyPlacement), nameof(LegacyPlacement.StorageIndex));
            int buckets = 0;
            for (int i = 0; i + 2 < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldelem_Ref || !codes[i + 2].Calls(add)) continue;
                var convert = new CodeInstruction(OpCodes.Call, storageIndex);
                convert.labels.AddRange(codes[i].labels);
                codes[i].labels.Clear();
                codes.Insert(i++, convert);
                buckets++;
            }
            if (buckets != 2)
                throw new System.InvalidOperationException($"Expected two native lane bucket insertions; found {buckets}.");
            return codes;
        }
    }

    [HarmonyPatch]
    internal static class LegacyBeatmapTransformLanes
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BeatDataMirrorTransform), nameof(BeatDataMirrorTransform.CreateTransformedData));
            yield return AccessTools.Method(typeof(BeatmapDataObstaclesAndBombsTransform), nameof(BeatmapDataObstaclesAndBombsTransform.CreateTransformedData));
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            LegacyPlacement.ConvertLineIndexReads(instructions, nameof(LegacyPlacement.StorageIndex), 2);
    }

    [HarmonyPatch(typeof(BeatmapDataLoader.NotesInTimeRowProcessor), nameof(BeatmapDataLoader.NotesInTimeRowProcessor.ProcessBasicNotesInTimeRow))]
    internal static class LegacyCrossedNoteLanes
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            LegacyPlacement.ConvertLineIndexReads(instructions, nameof(LegacyPlacement.Decode), 4);
    }

    [HarmonyPatch(typeof(NoteData), nameof(NoteData.SetNoteFlipToNote))]
    internal static class LegacyNoteFlipSide
    {
        private static void Postfix(NoteData __instance, NoteData targetNote)
        {
            float fromLane = LegacyPlacement.Decode(__instance.lineIndex);
            float toLane = LegacyPlacement.Decode(targetNote.lineIndex);
            float fromLayer = LegacyPlacement.Decode((int)__instance.noteLineLayer);
            float toLayer = LegacyPlacement.Decode((int)targetNote.noteLineLayer);
            float side = fromLane > toLane ? 1f : -1f;
            if ((fromLane > toLane && fromLayer < toLayer) || (fromLane < toLane && fromLayer > toLayer)) side *= -1f;
            __instance.flipYSide = side;
        }
    }

    [HarmonyPatch(typeof(BeatmapDataLoader.NotesInTimeRowProcessor), nameof(BeatmapDataLoader.NotesInTimeRowProcessor.ProcessNotesInTimeRow))]
    internal static class LegacyNoteStacks
    {
        private static void Postfix(List<NoteData> notes)
        {
            if (!notes.Any(note => MappingExtensionsData.IsPrecisionValue(note.lineIndex))) return;
            foreach (var column in notes.GroupBy(note => LegacyPlacement.Decode(note.lineIndex)))
            {
                int layer = 0;
                foreach (var note in column.OrderBy(note => LegacyPlacement.Decode((int)note.noteLineLayer)))
                    note.SetNoteStartLineLayer((NoteLineLayer)layer++);
            }
        }
    }

    [HarmonyPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.GetNoteOffset))]
    internal static class LegacyNoteOffset
    {
        private static void Postfix(BeatmapObjectSpawnMovementData __instance, int noteLineIndex, NoteLineLayer noteLineLayer, ref Vector3 __result)
        {
            if (!MappingExtensionsData.IsPrecisionValue(noteLineIndex)) return;
            var offset = __instance.Get2DNoteOffset(noteLineIndex, noteLineLayer);
            __result = __instance._rightVec * offset.x + Vector3.up * offset.y;
        }
    }

    [HarmonyPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.Get2DNoteOffset))]
    internal static class LegacyNoteOffset2D
    {
        private static void Postfix(BeatmapObjectSpawnMovementData __instance, int noteLineIndex, ref Vector2 __result)
        {
            if (MappingExtensionsData.IsPrecisionValue(noteLineIndex))
                __result.x = (LegacyPlacement.Decode(noteLineIndex) - (__instance._noteLinesCount - 1f) * 0.5f) * __instance.noteLinesDistance;
        }
    }

    [HarmonyPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.LineYPosForLineLayer))]
    internal static class LegacyNoteLayerHeight
    {
        private static void Postfix(BeatmapObjectSpawnMovementData __instance, NoteLineLayer lineLayer, ref float __result)
        {
            if (LegacyPlacement.TryDecodeLayer((int)lineLayer, __instance._baseLinesYPos, __instance._topLinesYPos - __instance._upperLinesYPos, out float height))
                __result = height;
        }
    }

    [HarmonyPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.HighestJumpPosYForLineLayer))]
    internal static class LegacyNoteJumpHeight
    {
        private static void Postfix(BeatmapObjectSpawnMovementData __instance, NoteLineLayer lineLayer, ref float __result)
        {
            if (LegacyPlacement.TryDecodeLayer((int)lineLayer, __instance._baseLinesHighestJumpPosY, __instance._topLinesHighestJumpPosY - __instance._upperLinesHighestJumpPosY, out float height))
                __result = height + __instance._jumpOffsetY;
        }
    }
}
