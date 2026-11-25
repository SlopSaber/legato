using System.Collections.Generic;
using System.Reflection;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch]
    internal static class BeatmapCopies
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BeatDataMirrorTransform), nameof(BeatDataMirrorTransform.CreateTransformedData));
            yield return AccessTools.Method(typeof(BeatmapDataNoArrowsTransform), nameof(BeatmapDataNoArrowsTransform.CreateTransformedData));
            yield return AccessTools.Method(typeof(BeatmapDataObstaclesAndBombsTransform), nameof(BeatmapDataObstaclesAndBombsTransform.CreateTransformedData));
            yield return AccessTools.Method(typeof(BeatmapDataStaticLightsTransform), nameof(BeatmapDataStaticLightsTransform.CreateTransformedData));
        }

        private static void Postfix(BeatmapData beatmapData, MethodBase __originalMethod, ref BeatmapData __result)
        {
            if (beatmapData is not CustomBeatmapData source || __result is CustomBeatmapData)
            {
                return;
            }

            if (__originalMethod.DeclaringType == typeof(BeatDataMirrorTransform))
            {
                // The mirror transform reconstructs rotation events at the same array index.
                for (int index = 0; index < __result.beatmapEventData.Length; index++)
                {
                    BeatmapEventData item = __result.beatmapEventData[index];
                    if (item is not CustomBasicBeatmapEventData && source.beatmapEventData[index] is CustomBasicBeatmapEventData original)
                    {
                        __result.beatmapEventData[index] = original.Copy(item);
                    }
                }
            }

            __result = source.CopyDataTo(__result);
        }
    }

    [HarmonyPatch(typeof(BeatmapData), nameof(BeatmapData.GetCopy))]
    internal static class CopyBeatmapData
    {
        private static void Postfix(BeatmapData __instance, ref BeatmapData __result)
        {
            if (__instance is CustomBeatmapData source)
            {
                __result = source.CopyDataTo(__result);
            }
        }
    }
}
