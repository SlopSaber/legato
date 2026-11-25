#if !PRE_V1_37_1
using System.Threading.Tasks;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.LoadBeatmapDataAsync))]
    internal static class BeatmapDataLoaderLevelInfoInjectorAsync
    {
        internal static void AttachCustomData(IReadonlyBeatmapData result, IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey)
        {
            if (beatmapLevelData is not CustomFileBeatmapLevelData fileBeatmapLevelData ||
                result is not CustomBeatmapData beatmapData)
            {
                return;
            }

            beatmapData.levelCustomData = fileBeatmapLevelData.customData;
            FileDifficultyBeatmap? fileDifficultyBeatmap = fileBeatmapLevelData.GetDifficultyBeatmap(beatmapKey);
            if (fileDifficultyBeatmap is CustomFileDifficultyBeatmap customFileDifficultyBeatmap)
            {
                beatmapData.beatmapCustomData = customFileDifficultyBeatmap.customData;
            }
        }

        [HarmonyPostfix]
        private static void InjectCustomData(ref Task<IReadonlyBeatmapData> __result, IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey)
        {
            __result = InjectCustomDataAsync(__result, beatmapLevelData, beatmapKey);
        }

        private static async Task<IReadonlyBeatmapData> InjectCustomDataAsync(Task<IReadonlyBeatmapData> task, IBeatmapLevelData beatmapLevelData, BeatmapKey beatmapKey)
        {
            IReadonlyBeatmapData result = await task;
            AttachCustomData(result, beatmapLevelData, beatmapKey);
            return result;
        }
    }
}
#endif
