#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck;
using static NoodleExtensions.NoodleController;

namespace NoodleExtensions.HarmonyPatches.FakeNotes;

[HeckPatch]
internal static class LegacyFakeObjectCounts
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CustomBeatmapData), nameof(CustomBeatmapData.ApplyLevelMetadata))]
    private static void ApplyCounts(CustomBeatmapData __instance)
    {
        if (__instance.beatmapCustomData.Get<List<object>>("_requirements")?.Contains(CAPABILITY) != true)
        {
            return;
        }

        int notes = 0;
        int bombs = 0;
        int obstacles = 0;
        foreach (BeatmapObjectData item in __instance.beatmapObjectDatas)
        {
            if (item is ICustomData data && data.customData.TryGetValue(V2_FAKE_NOTE, out object? fake) && fake is true)
            {
                continue;
            }

            switch (item)
            {
                case NoteData { noteType: NoteType.NoteA or NoteType.NoteB }:
                    notes++;
                    break;
                case NoteData { noteType: NoteType.Bomb }:
                    bombs++;
                    break;
                case ObstacleData:
                    obstacles++;
                    break;
            }
        }

        __instance.notesCount = notes;
        __instance.bombsCount = bombs;
        __instance.obstaclesCount = obstacles;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CustomBeatmapData), "CopyDataTo")]
    private static void CountCopy(CustomBeatmapData __result) => ApplyCounts(__result);
}
#endif
