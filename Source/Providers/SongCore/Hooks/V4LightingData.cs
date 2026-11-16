#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CustomJSONData.CustomBeatmap;
using Legato.Beatmaps.Formats;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json.Linq;
using Zenject;
using SaveData = BeatmapSaveDataVersion3.BeatmapSaveData;

namespace SongCore.Hooks
{
    internal sealed class V4LightingData : IInitializable, IDisposable
    {
        private static readonly ConditionalWeakTable<SaveData.LightColorEventBox, Metadata[]> Saved = new();
        private static readonly ConditionalWeakTable<LightColorBaseData, Metadata> Converted = new();
        private Hook? _convert;
        private Hook? _unpack;

        public void Initialize()
        {
            _convert = new Hook(typeof(BeatmapDataLoader.LightColorEventBoxConvertor).GetMethod(nameof(BeatmapDataLoader.LightColorEventBoxConvertor.Convert))!, ConvertBox, true);
            _unpack = new Hook(typeof(LightColorBeatmapEventDataBox).GetMethod(nameof(LightColorBeatmapEventDataBox.Unpack))!, UnpackBox, true);
        }

        public void Dispose() { _unpack?.Dispose(); _convert?.Dispose(); }

        internal static void Register(SaveData save, V4LegacyProjection projection)
        {
            JObject[] groups = V4Documents.Objects(projection.Lightshow, "eventBoxGroups").Where(value => V4Documents.Index(value["t"], "eventBoxGroups.t") == 1).ToArray();
            if (groups.Length != save.lightColorEventBoxGroups.Count) throw new InvalidOperationException("The v4 color group projection changed ordering.");
            for (int group = 0; group < groups.Length; group++)
            {
                JObject[] boxes = V4Documents.Objects(groups[group], "e").ToArray();
                var savedBoxes = save.lightColorEventBoxGroups[group].eventBoxes;
                if (boxes.Length != savedBoxes.Count) throw new InvalidOperationException("The v4 color box projection changed ordering.");
                for (int box = 0; box < boxes.Length; box++)
                {
                    Metadata[] events = V4Documents.Objects(boxes[box], "events").Select(value =>
                    {
                        JObject data = V4Documents.Object(value["data"], "lightColorEvents.data");
                        double ease = V4Documents.Number(data["e"], 0, "lightColorEvents.e");
                        if (Math.Truncate(ease) != ease || ease < int.MinValue || ease > int.MaxValue)
                            throw new System.IO.InvalidDataException("Invalid v4 light-color easing value.");
                        return new Metadata(BeatmapDataLoader.ConvertEaseType((SaveData.EaseType)(int)ease),
                            (float)V4Documents.Number(data["sb"], 0, "lightColorEvents.sb"), V4Documents.Number(data["sf"], 0, "lightColorEvents.sf") != 0);
                    }).ToArray();
                    if (events.Length != savedBoxes[box].lightColorBaseDataList.Count) throw new InvalidOperationException("The v4 color event projection changed ordering.");
                    Saved.Add(savedBoxes[box], events);
                }
            }
        }

        private LightColorBeatmapEventDataBox ConvertBox(Func<BeatmapDataLoader.LightColorEventBoxConvertor, SaveData.LightColorEventBox, LightGroupSO, LightColorBeatmapEventDataBox> original,
            BeatmapDataLoader.LightColorEventBoxConvertor converter, SaveData.LightColorEventBox source, LightGroupSO group)
        {
            var result = original(converter, source, group);
            if (Saved.TryGetValue(source, out Metadata[] metadata))
            {
                if (metadata.Length != result._lightColorBaseDataList.Count) throw new InvalidOperationException("Native light conversion changed v4 event ordering.");
                for (int index = 0; index < metadata.Length; index++) Converted.Add(result._lightColorBaseDataList[index], metadata[index]);
            }
            return result;
        }

        private void UnpackBox(Action<LightColorBeatmapEventDataBox, float, int, int, int, int, float, IBeatToTimeConvertor, List<BeatmapEventData>> original,
            LightColorBeatmapEventDataBox box, float beat, int group, int element, int durationIndex, int distributionIndex, float maxBeat, IBeatToTimeConvertor tempo, List<BeatmapEventData> output)
        {
            int start = output.Count;
            original(box, beat, group, element, durationIndex, distributionIndex, maxBeat, tempo, output);
            int added = 0;
            foreach (LightColorBaseData source in box._lightColorBaseDataList)
            {
                if (beat + source.beat + durationIndex * box._beatStep >= maxBeat) continue;
                if (start + added >= output.Count) throw new InvalidOperationException("Native light unpacking changed v4 event ordering.");
                if (Converted.TryGetValue(source, out Metadata metadata))
                {
                    var current = (LightColorBeatmapEventData)output[start + added];
                    CustomData custom = current is ICustomData customEvent ? customEvent.customData.Copy() : new CustomData();
                    output[start + added] = new Event(current, custom, metadata);
                }
                added++;
            }
        }

        internal static void ResolvePrevious(BeatmapData data)
        {
            foreach (BeatmapDataItem item in data.allBeatmapDataItems)
                if (item is Event current && (int)current.transitionType == 2)
                {
                    var previous = current.prevSameTypeEventData as Event;
                    current.Details = new Metadata(current.Details.Ease, previous?.Details.StrobeBrightness ?? 0, previous?.Details.StrobeFade ?? false);
                }
        }

        internal sealed class Metadata
        {
            internal readonly EaseType Ease;
            internal readonly float StrobeBrightness;
            internal readonly bool StrobeFade;
            internal Metadata(EaseType ease, float brightness, bool fade) { Ease = ease; StrobeBrightness = brightness; StrobeFade = fade; }
        }

        internal sealed class Event : LightColorBeatmapEventData, ICustomData
        {
            internal Metadata Details;
            public CustomData customData { get; }
            internal Event(LightColorBeatmapEventData source, CustomData custom, Metadata metadata)
                : base(source.time, source.groupId, source.elementId, source.transitionType, source.colorType, source.brightness, source.strobeBeatFrequency)
            { customData = custom; Details = metadata; }
            public override BeatmapDataItem GetCopy() => new Event(this, customData.Copy(), Details);
        }
    }
}
#endif
