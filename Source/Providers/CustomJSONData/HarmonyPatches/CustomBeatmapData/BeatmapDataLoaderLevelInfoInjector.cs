#if !PRE_V1_37_1 && !BEAT_SABER_1453
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch]
    internal static class BeatmapDataLoaderLevelInfoInjector
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeatmapDataLoader), "LoadBeatmapData")]
        private static void InjectCustomData(IReadonlyBeatmapData __result, IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey)
        {
            BeatmapDataLoaderLevelInfoInjectorAsync.AttachCustomData(__result, beatmapLevelData, beatmapKey);
        }
    }
}
#endif
