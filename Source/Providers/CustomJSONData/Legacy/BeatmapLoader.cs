using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomJSONData.HarmonyPatches
{
    [HarmonyPatch(typeof(BeatmapDataLoader), nameof(BeatmapDataLoader.GetBeatmapDataFromJson))]
    internal static class BeatmapLoader
    {
#if LEGATO_CUSTOM_JSON_EARLY_LOADER
        private static readonly Func<float, float, float, float, float> _convertTime =
            (Func<float, float, float, float, float>)Delegate.CreateDelegate(
                typeof(Func<float, float, float, float, float>),
                AccessTools.Method(typeof(BeatmapDataLoader), "GetRealTimeFromBpmTime"));
#else
        private static readonly TimeConverter _convertTime = (TimeConverter)Delegate.CreateDelegate(
            typeof(TimeConverter), AccessTools.Method(typeof(BeatmapDataLoader), "GetRealTimeFromBPMTime"));

        private delegate float TimeConverter(BeatmapDataLoader loader, float beat, float bpm, float shuffle, float shufflePeriod);

#endif

        internal static CustomData ReadData(JToken? token)
        {
            if (token is not JObject data)
            {
                return new CustomData();
            }

            using JsonReader reader = data.CreateReader();
            reader.Read();
            return CustomData.FromJSON(reader);
        }

#if LEGATO_CUSTOM_JSON_EARLY_LOADER
        private static void Postfix(string json, float beatsPerMinute, float shuffle, float shufflePeriod, ref BeatmapData? __result)
#else
        private static void Postfix(BeatmapDataLoader __instance, string json, float startBPM, float shuffle, float shufflePeriod, ref BeatmapData? __result)
#endif
        {
            if (__result == null || __result is CustomBeatmapData)
            {
                return;
            }

            try
            {
                using JsonTextReader reader = new(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
                JObject chart = JObject.Load(reader);
                if (chart.Property("version") != null || chart["_notes"] is not JArray notes)
                {
                    return;
                }

                Version version = Version.TryParse((string?)chart["_version"], out Version parsedVersion) ? parsedVersion : VersionExtensions.version2;
                if (!version.IsVersion2())
                {
                    return;
                }

                JArray obstacles = chart["_obstacles"] as JArray ?? new JArray();
                JArray events = chart["_events"] as JArray ?? new JArray();
                JObject? chartData = chart["_customData"] as JObject;
#if LEGATO_CUSTOM_JSON_EARLY_LOADER
                IReadOnlyList<CustomEventData> customEvents = ReadEvents(chartData?["_customEvents"] as JArray, beatsPerMinute, shuffle, shufflePeriod, version);
#else
                IReadOnlyList<CustomEventData> customEvents = ReadEvents(chartData?["_customEvents"] as JArray, events, __instance, startBPM, shuffle, shufflePeriod, version);
#endif
                CustomData customData = ReadChartData(chartData, version);

                foreach (BeatmapLineData line in __result.beatmapLinesData)
                {
                    for (int index = 0; index < line.beatmapObjectsData.Length; index++)
                    {
                        // The native loader numbers notes first, then obstacles, before sorting each lane.
                        line.beatmapObjectsData[index] = line.beatmapObjectsData[index] switch
                        {
                            NoteData note when note.id >= 0 && note.id < notes.Count => new CustomNoteData(note, ReadData((notes[note.id] as JObject)?["_customData"]), version),
                            ObstacleData obstacle when obstacle.id >= notes.Count && obstacle.id - notes.Count < obstacles.Count => new CustomObstacleData(obstacle, ReadData((obstacles[obstacle.id - notes.Count] as JObject)?["_customData"]), version),
                            BeatmapObjectData item => item
                        };
                    }
                }

                BeatmapEventData[] nativeEvents = __result.beatmapEventData;
                if (events.Count == nativeEvents.Length || events.Count == 0)
                {
                    for (int index = 0; index < nativeEvents.Length; index++)
                    {
                        JObject? source = index < events.Count ? events[index] as JObject : null;
                        nativeEvents[index] = new CustomBasicBeatmapEventData(nativeEvents[index], ReadNumber(source?["_floatValue"], 1), ReadData(source?["_customData"]), version);
                    }
                }

                __result = new CustomBeatmapData(__result, customData, version, customEvents)
                {
                    hasAuthoredEvents = events.Count > 0
                };
            }
            catch (JsonException exception)
            {
                Plugin.Log.Warn($"Could not read beatmap custom data: {exception.Message}");
            }
        }

#if LEGATO_CUSTOM_JSON_EARLY_LOADER
        private static IReadOnlyList<CustomEventData> ReadEvents(JArray? customEvents, float startBpm, float shuffle, float shufflePeriod, Version version)
#else
        private static IReadOnlyList<CustomEventData> ReadEvents(JArray? customEvents, JArray events, BeatmapDataLoader loader, float startBpm, float shuffle, float shufflePeriod, Version version)
#endif
        {
            if (customEvents == null || customEvents.Count == 0)
            {
                return Array.Empty<CustomEventData>();
            }

#if !LEGATO_CUSTOM_JSON_EARLY_LOADER
            List<(float Beat, float Time, float Bpm)> changes = new() { (0, 0, startBpm) };
            foreach (JObject item in events.OfType<JObject>())
            {
                if (item["_type"] is JValue { Type: JTokenType.Integer } type && ((BeatmapEventType)type.Value<int>()).IsBPMChangeEvent())
                {
                    float beat = ReadNumber(item["_time"]);
                    float bpm = ReadNumber(item["_value"]);

                    // The 1.11.1 loader starts each change at the time calculated with the new BPM from beat zero.
                    changes.Add((beat, _convertTime(loader, beat, bpm, shuffle, shufflePeriod), bpm));
                }
            }

#endif
            List<CustomEventData> result = new();
            foreach (JObject item in customEvents.OfType<JObject>())
            {
                if (item["_type"] is not JValue { Type: JTokenType.String, Value: string type } || string.IsNullOrEmpty(type))
                {
                    continue;
                }

                float beat = ReadNumber(item["_time"]);
#if LEGATO_CUSTOM_JSON_EARLY_LOADER
                float seconds = _convertTime(beat, startBpm, shuffle, shufflePeriod);
                result.Add(new CustomEventData(seconds, type, ReadData(item["_data"]), version, startBpm));
#else
                int changeIndex = 0;
                while (changeIndex + 1 < changes.Count && changes[changeIndex + 1].Beat < beat)
                {
                    changeIndex++;
                }

                (float startBeat, float startTime, float bpm) = changes[changeIndex];
                float seconds = startTime + _convertTime(loader, beat - startBeat, bpm, shuffle, shufflePeriod);
                result.Add(new CustomEventData(seconds, type, ReadData(item["_data"]), version, bpm));
#endif
            }

            return result.OrderBy(item => item.time).ToArray();
        }

        private static CustomData ReadChartData(JObject? data, Version version)
        {
            if (data == null)
            {
                return new CustomData();
            }

            using JsonReader reader = data.CreateReader();
            reader.Read();
            object[] inputs = { reader, version };
            return CustomData.FromJSON(reader, name =>
            {
                if (name != "_customEvents")
                {
                    return CustomJSONDataDeserializer.Activate(inputs, name);
                }

                reader.Skip();
                return false;
            });
        }

        private static float ReadNumber(JToken? token, float fallback = 0)
        {
            return token is JValue { Type: JTokenType.Integer or JTokenType.Float } number ? number.Value<float>() : fallback;
        }
    }
}
