using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomJSONData.CustomBeatmap
{
    public sealed class CustomBeatmapData : BeatmapData, ICustomData, IVersionable
    {
        private static readonly PropertyInfo[] _nativeChartProperties = new[]
        {
            nameof(beatmapLinesData), nameof(beatmapEventData), nameof(notesCount),
            nameof(obstaclesCount), nameof(bombsCount), nameof(spawnRotationEventsCount)
        }.Select(name => typeof(BeatmapData).GetProperty(name)).ToArray();

        internal CustomBeatmapData(
            BeatmapData original,
            CustomData data,
            Version version,
            IReadOnlyList<CustomEventData> events)
            : base(original.beatmapLinesData, original.beatmapEventData)
        {
            customData = data;
            this.version = version;
            customEventDatas = events;
            beatmapObjectDatas = beatmapLinesData.SelectMany(line => line.beatmapObjectsData).OrderBy(item => item.time).ToArray();
        }

        public CustomData customData { get; private set; }

        public CustomData beatmapCustomData { get; internal set; } = new();

        public CustomData levelCustomData { get; internal set; } = new();

        public Version version { get; private set; }

        public bool hasAuthoredEvents { get; internal set; }

        public IReadOnlyList<CustomEventData> customEventDatas { get; private set; }

        public IReadOnlyList<BeatmapObjectData> beatmapObjectDatas { get; private set; }

        public IReadOnlyList<BeatmapEventData> beatmapEventDatas => beatmapEventData;

        public void ApplyLevelMetadata(CustomLevelInfoSaveData info, CustomLevelInfoSaveData.DifficultyBeatmap difficulty)
        {
            levelCustomData = info.customData.Copy();
            beatmapCustomData = difficulty.customData.Copy();
        }

        public void ReplaceChartFrom(CustomBeatmapData chart)
        {
            foreach (PropertyInfo property in _nativeChartProperties)
            {
                property.SetValue(this, property.GetValue(chart));
            }

            customData = chart.customData;
            version = chart.version;
            customEventDatas = chart.customEventDatas;
            beatmapObjectDatas = chart.beatmapObjectDatas;
            levelCustomData = chart.levelCustomData;
            beatmapCustomData = chart.beatmapCustomData;
            hasAuthoredEvents = chart.hasAuthoredEvents;
        }

        public CustomBeatmapData CopyWithEvents(BeatmapEventData[] events) => CopyDataTo(new BeatmapData(beatmapLinesData, events));

        internal CustomBeatmapData CopyDataTo(BeatmapData result)
        {
            return new CustomBeatmapData(result, customData.Copy(), version, customEventDatas.Select(item => item.GetCopy()).ToArray())
            {
                beatmapCustomData = beatmapCustomData.Copy(),
                levelCustomData = levelCustomData.Copy(),
                hasAuthoredEvents = hasAuthoredEvents
            };
        }
    }

    public sealed class CustomEventData : ICustomData, IVersionable
    {
        public CustomEventData(float time, string type, CustomData data, Version version)
            : this(time, type, data, version, null)
        {
        }

        internal CustomEventData(float time, string type, CustomData data, Version version, float? beatsPerMinute)
        {
            this.time = time;
            eventType = type;
            customData = data;
            this.version = version;
            this.beatsPerMinute = beatsPerMinute;
        }

        public float time { get; }

        public string eventType { get; }

        public CustomData customData { get; }

        public Version version { get; }

        public float? beatsPerMinute { get; }

        public CustomEventData GetCopy() => new(time, eventType, customData.Copy(), version, beatsPerMinute);
    }

    public sealed class CustomNoteData : NoteData, ICustomData, IVersionable
    {
        internal CustomNoteData(NoteData note, CustomData data, Version version)
            : base(note.id, note.time, note.lineIndex, note.noteLineLayer, note.startNoteLineLayer, note.noteType, note.cutDirection, note.timeToNextBasicNote, note.timeToPrevBasicNote, note.flipLineIndex, note.flipYSide)
        {
            customData = data;
            this.version = version;
        }

        public CustomData customData { get; }

        public Version version { get; }

        public override BeatmapObjectData GetCopy() => new CustomNoteData(this, customData.Copy(), version);
    }

    public sealed class CustomObstacleData : ObstacleData, ICustomData, IVersionable
    {
        internal CustomObstacleData(ObstacleData obstacle, CustomData data, Version version)
            : base(obstacle.id, obstacle.time, obstacle.lineIndex, obstacle.obstacleType, obstacle.duration, obstacle.width)
        {
            customData = data;
            this.version = version;
        }

        public CustomData customData { get; }

        public Version version { get; }

        public override BeatmapObjectData GetCopy() => new CustomObstacleData(this, customData.Copy(), version);
    }

    public sealed class CustomBasicBeatmapEventData : BeatmapEventData, ICustomData, IVersionable
    {
        public CustomBasicBeatmapEventData(BeatmapEventData item, float floatValue, CustomData data, Version version)
            : base(item.time, item.type, item.value)
        {
            this.floatValue = floatValue;
            customData = data;
            this.version = version;
        }

        public float floatValue { get; }

        public CustomData customData { get; }

        public Version version { get; }

        internal CustomBasicBeatmapEventData Copy(BeatmapEventData item) => new(item, floatValue, customData.Copy(), version);
    }
}
