using System;
using System.IO;
using System.Runtime.CompilerServices;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch]
    internal static class EarlyLevelMetadata
    {
        private static readonly ConditionalWeakTable<CustomLevelInfo.DifficultyLevelInfo, Metadata> _levels = new();
        private static readonly ConditionalWeakTable<BeatmapDataSO, Metadata> _beatmaps = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomLevelLoader), nameof(CustomLevelLoader.LoadCustomLevelInfo))]
        private static void ReadLevelInfo(string path, string jsonFile, CustomLevelInfo? __result)
        {
            if (__result?.difficultyLevels == null)
            {
                return;
            }

            string file = Path.Combine(path, jsonFile);
            try
            {
                using StreamReader input = File.OpenText(file);
                using JsonTextReader reader = new(input) { DateParseHandling = DateParseHandling.None };
                JObject info = JObject.Load(reader);
                if (info["difficultyLevels"] is not JArray difficulties)
                {
                    return;
                }

                CustomData level = BeatmapLoader.ReadData(info["_customData"] ?? info["customData"]);
                int count = Math.Min(difficulties.Count, __result.difficultyLevels.Length);
                for (int index = 0; index < count; index++)
                {
                    if (__result.difficultyLevels[index] is CustomLevelInfo.DifficultyLevelInfo difficulty &&
                        difficulties[index] is JObject document)
                    {
                        CustomData data = BeatmapLoader.ReadData(document["_customData"] ?? document["customData"]);
                        _levels.GetValue(difficulty, _ => new Metadata(level, data));
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                Plugin.Log.Warn($"Could not read custom level data from '{file}': {exception.Message}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomLevelStandardLevelLoader), "Load")]
        private static void TrackBeatmap(CustomLevelInfo.DifficultyLevelInfo difficultyLevelsInfo, BeatmapDataSO beatmapData, StandardLevelSO.DifficultyBeatmap? __result)
        {
            if (__result != null && _levels.TryGetValue(difficultyLevelsInfo, out Metadata metadata))
            {
                _beatmaps.GetValue(beatmapData, _ => metadata);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeatmapDataSO), nameof(BeatmapDataSO.Load))]
        private static void ApplyMetadata(BeatmapDataSO __instance, BeatmapData? ____beatmapData)
        {
            if (____beatmapData is CustomBeatmapData chart && _beatmaps.TryGetValue(__instance, out Metadata metadata))
            {
                chart.ApplyLevelMetadata(metadata.Level, metadata.Difficulty);
            }
        }

        private sealed class Metadata
        {
            internal Metadata(CustomData level, CustomData difficulty)
            {
                Level = level;
                Difficulty = difficulty;
            }

            internal CustomData Level { get; }

            internal CustomData Difficulty { get; }
        }
    }
}
