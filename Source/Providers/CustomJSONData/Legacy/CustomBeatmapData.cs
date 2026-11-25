using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomJSONData.CustomBeatmap
{
    public sealed class CustomBeatmapData : BeatmapData, ICustomData, IVersionable
    {
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

        public CustomData customData { get; }

        public CustomData beatmapCustomData { get; internal set; } = new();

        public CustomData levelCustomData { get; internal set; } = new();

        public Version version { get; }

        public IReadOnlyList<CustomEventData> customEventDatas { get; }

        public IReadOnlyList<BeatmapObjectData> beatmapObjectDatas { get; }

        public IReadOnlyList<BeatmapEventData> beatmapEventDatas => beatmapEventData;

        internal CustomBeatmapData CopyDataTo(BeatmapData result)
        {
            return new CustomBeatmapData(result, customData.Copy(), version, customEventDatas.Select(item => item.GetCopy()).ToArray())
            {
                beatmapCustomData = beatmapCustomData.Copy(),
                levelCustomData = levelCustomData.Copy()
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
        internal CustomBasicBeatmapEventData(BeatmapEventData item, float floatValue, CustomData data, Version version)
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
