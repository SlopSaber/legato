#if LEGATO_CHROMA_LEGACY_LIGHTS
using System.Collections.Generic;

namespace Chroma.Lighting;

internal enum LegacyEnvironmentColorType
{
    Color0,
    Color1,
    ColorW
}

internal static class LegacyLightEventValues
{
    internal static EnvironmentColorType GetLightColorTypeFromEventDataValue(int value)
    {
        return value switch
        {
            >= 5 and <= 8 => EnvironmentColorType.Color1,
            >= 9 and <= 12 => EnvironmentColorType.ColorW,
            _ => EnvironmentColorType.Color0
        };
    }

    internal static bool HasLightFadeEventDataValue(this BeatmapEventData item) => item.value is 4 or 8 or 12;

    internal static bool HasFixedDurationLightSwitchEventDataValue(this BeatmapEventData item) => item.value is -1 or 2 or 3 or 6 or 7 or 10 or 11;
}

internal sealed class LegacyLightTimeline
{
    private readonly BeatmapData _chart;
    private readonly GameplayCoreSceneSetupData _setup;
    private readonly Dictionary<BeatmapEventData, BeatmapEventData> _next = new();
    private BeatmapEventData[]? _lastEvents;

    private LegacyLightTimeline(BeatmapData chart, GameplayCoreSceneSetupData setup)
    {
        _chart = chart;
        _setup = setup;
    }

    internal BeatmapEventData? NextEvent(BeatmapEventData previous)
    {
        BeatmapEventData[] current = _chart.beatmapEventData;
        if (!ReferenceEquals(_lastEvents, current))
        {
            _next.Clear();
            var lastByType = new Dictionary<BeatmapEventType, BeatmapEventData>();
            for (int index = current.Length - 1; index >= 0; index--)
            {
                BeatmapEventData item = current[index];
                if (lastByType.TryGetValue(item.type, out BeatmapEventData next))
                {
                    _next.Add(item, next);
                }

                lastByType[item.type] = item;
            }

            _lastEvents = current;
        }

        return _next.TryGetValue(previous, out BeatmapEventData result) ? result : null;
    }

    internal float TempoAt(float time)
    {
        float bpm = _setup.difficultyBeatmap.level.beatsPerMinute;
        foreach (BeatmapEventData item in _chart.beatmapEventData)
        {
            if (item.time >= time)
            {
                break;
            }

            if (item.type.IsBPMChangeEvent() && item.value > 0)
            {
                bpm = item.value;
            }
        }

        return bpm;
    }
}
#endif
