#if LEGATO_HECK_LEGACY_GAMEPLAY
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using SiraUtil.Logging;

namespace Heck.ReLoad;

public class ReLoaderLoader
{
    private static readonly FieldInfo _chartField = AccessTools.Field(typeof(CustomDifficultyBeatmap), "_beatmapData");
    private readonly SiraLog _log;

    private ReLoaderLoader(SiraLog log)
    {
        _log = log;
    }

    public void Reload(IDifficultyBeatmap selected)
    {
        if (selected is CustomDifficultyBeatmap difficulty && ReadChart(selected) is { } chart)
        {
            ReplaceOriginal(difficulty, chart);
        }
    }

    internal CustomBeatmapData? ReadChart(IDifficultyBeatmap selected)
    {
        if (selected is not CustomDifficultyBeatmap || selected.level is not CustomPreviewBeatmapLevel level)
        {
            _log.Error("Cannot reload a non-custom beatmap");
            return null;
        }

        StandardLevelInfoSaveData info = level.standardLevelInfoSaveData;
        string characteristic = selected.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
        string difficulty = selected.difficulty.ToString();
        StandardLevelInfoSaveData.DifficultyBeatmap? metadata = info.difficultyBeatmapSets
            .FirstOrDefault(set => set.beatmapCharacteristicName == characteristic)?.difficultyBeatmaps
            .FirstOrDefault(item => item.difficulty == difficulty);
        if (metadata == null)
        {
            _log.Error("Could not find the selected chart in Info.dat");
            return null;
        }

        try
        {
            string json = File.ReadAllText(Path.Combine(level.customLevelPath, metadata.beatmapFilename));
            BeatmapData result = new BeatmapDataLoader().GetBeatmapDataFromJson(json, info.beatsPerMinute, info.shuffle, info.shufflePeriod);
            if (result is not CustomBeatmapData chart)
            {
                _log.Error("The updated chart could not be read as custom v2 data");
                return null;
            }

            if (info is CustomLevelInfoSaveData levelInfo && metadata is CustomLevelInfoSaveData.DifficultyBeatmap difficultyInfo)
            {
                chart.ApplyLevelMetadata(levelInfo, difficultyInfo);
            }

            return chart;
        }
        catch (Exception exception)
        {
            _log.Error($"Could not reload the chart: {exception.Message}");
            return null;
        }
    }

    internal void ReplaceOriginal(CustomDifficultyBeatmap difficulty, CustomBeatmapData chart)
    {
        _chartField.SetValue(difficulty, chart);
        _log.Trace("Reloaded beatmap");
    }
}
#endif
