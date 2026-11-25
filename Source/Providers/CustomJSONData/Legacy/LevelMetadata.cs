using System.Threading.Tasks;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch(typeof(CustomLevelLoader), "LoadDifficultyBeatmapAsync")]
    internal static class LevelMetadata
    {
        private static void Postfix(
            StandardLevelInfoSaveData standardLevelInfoSaveData,
            StandardLevelInfoSaveData.DifficultyBeatmap difficultyBeatmapSaveData,
            ref Task<CustomDifficultyBeatmap> __result)
        {
            if (standardLevelInfoSaveData is CustomLevelInfoSaveData info && difficultyBeatmapSaveData is CustomLevelInfoSaveData.DifficultyBeatmap difficulty)
            {
                __result = AttachMetadata(__result, info, difficulty);
            }
        }

        private static async Task<CustomDifficultyBeatmap> AttachMetadata(
            Task<CustomDifficultyBeatmap> load,
            CustomLevelInfoSaveData info,
            CustomLevelInfoSaveData.DifficultyBeatmap difficulty)
        {
            CustomDifficultyBeatmap result = await load.ConfigureAwait(false);
            if (result?.beatmapData is CustomBeatmapData beatmap)
            {
                beatmap.ApplyLevelMetadata(info, difficulty);
            }

            return result!;
        }
    }
}
