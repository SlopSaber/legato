#nullable enable
using System;
using Newtonsoft.Json.Linq;

namespace Legato.Beatmaps.Formats
{
    internal sealed class V4LegacyProjection
    {
        internal JObject Gameplay { get; }
        internal JObject Lightshow { get; }
        internal JObject Audio { get; }
        internal V4TempoMap Tempo { get; }
        internal JObject SaveData { get; }

        internal V4LegacyProjection(JObject gameplay, JObject lightshow, JObject audio, double bpm)
        {
            Gameplay = V4Documents.ExpandGameplay(gameplay);
            Lightshow = V4Documents.ExpandLightshow(lightshow);
            Audio = audio;
            Tempo = new V4TempoMap(audio, bpm);
            SaveData = new JObject
            {
                ["version"] = "3.3.0",
                ["colorNotes"] = Items(Gameplay, "colorNotes", "x", "y", "c", "d", "a"),
                ["bombNotes"] = Items(Gameplay, "bombNotes", "x", "y"),
                ["obstacles"] = Items(Gameplay, "obstacles", "x", "y", "d", "w", "h"),
                ["sliders"] = Sliders(false),
                ["burstSliders"] = Sliders(true),
                ["bpmEvents"] = BpmEvents(bpm),
                ["rotationEvents"] = new JArray(),
                ["waypoints"] = Items(Lightshow, "waypoints", "x", "y", "d"),
                ["basicBeatmapEvents"] = Items(Lightshow, "basicEvents", "et", "i", "f"),
                ["colorBoostBeatmapEvents"] = Items(Lightshow, "colorBoostEvents", "o"),
                ["lightColorEventBoxGroups"] = Groups(1),
                ["lightRotationEventBoxGroups"] = Groups(2),
                ["lightTranslationEventBoxGroups"] = Groups(3),
                ["basicEventTypesWithKeywords"] = Lightshow["basicEventTypesWithKeywords"]?.DeepClone(),
                ["useNormalEventsAsCompatibleEvents"] = Lightshow["useNormalEventsAsCompatibleEvents"]?.DeepClone() ?? new JValue(false),
                ["customData"] = MergeCustomData(Lightshow["customData"], Gameplay["customData"])
            };
            NormalizeNumbers(SaveData);
        }

        private static void NormalizeNumbers(JToken token)
        {
            for (JToken? child = token.First; child != null;)
            {
                JToken? next = child.Next;
                if (child is JProperty property && property.Name == "customData") { child = next; continue; }
                if (child is JValue number && number.Type == JTokenType.Float)
                {
                    double value = number.Value<double>();
                    if (value >= long.MinValue && value < (double)long.MaxValue && value == Math.Truncate(value))
                        number.Replace(new JValue((long)value));
                }
                else NormalizeNumbers(child);
                child = next;
            }
        }

        // Retain the expanded source because v3 JSON cannot express per-object lane
        // rotations, variable NJS, FX or arbitrary light-color easing.
        private static JObject Metadata(JObject item)
        {
            var result = new JObject();
            if (item["data"] is JObject data && data["customData"] is JObject dataCustom)
                result.Merge(dataCustom.DeepClone());
            if (item["customData"] is JObject objectCustom)
                result.Merge(objectCustom.DeepClone(), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            return result;
        }

        private static JObject MergeCustomData(JToken? first, JToken? second)
        {
            JObject result = first is JObject source ? (JObject)source.DeepClone() : new JObject();
            if (second is JObject extra)
                result.Merge(extra.DeepClone(), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });
            return result;
        }

        private static JArray Items(JObject source, string name, params string[] fields)
        {
            var result = new JArray();
            foreach (JObject item in V4Documents.Objects(source, name))
            {
                var data = V4Documents.Object(item["data"], name + ".data");
                var entry = new JObject { ["b"] = Number(item, "b"), ["customData"] = Metadata(item) };
                foreach (string field in fields)
                    entry[field] = name == "colorBoostEvents" ? new JValue(Number(data, "b") != 0)
                        : new JValue(Number(data, name == "basicEvents" && field == "et" ? "t" : field));
                if (name == "colorNotes" || name == "bombNotes" || name == "obstacles")
                    ((JObject)entry["customData"]!)["legatoV4Rotation"] = Number(item, "r");
                result.Add(entry);
            }
            return result;
        }

        private JArray Sliders(bool chain)
        {
            var result = new JArray();
            foreach (JObject item in V4Documents.Objects(Gameplay, chain ? "chains" : "arcs"))
            {
                var data = V4Documents.Object(item["data"], "slider.data");
                var head = V4Documents.Object(item["headData"], "slider.headData");
                JObject tail = chain ? data : V4Documents.Object(item["tailData"], "slider.tailData");
                var entry = new JObject
                {
                    ["b"] = Number(item, "hb"), ["tb"] = Number(item, "tb"),
                    ["x"] = Number(head, "x"), ["y"] = Number(head, "y"),
                    ["c"] = Number(head, "c"), ["d"] = Number(head, "d"),
                    ["tx"] = Number(tail, chain ? "tx" : "x"), ["ty"] = Number(tail, chain ? "ty" : "y"),
                    ["customData"] = Metadata(item)
                };
                ((JObject)entry["customData"]!)["legatoV4Rotation"] = Number(item, "hr");
                ((JObject)entry["customData"]!)["legatoV4TailRotation"] = Number(item, "tr");
                if (chain)
                {
                    entry["sc"] = Number(data, "c");
                    entry["s"] = Number(data, "s");
                }
                else
                {
                    entry["mu"] = Number(data, "m"); entry["tmu"] = Number(data, "tm");
                    entry["tc"] = Number(tail, "d"); entry["m"] = Number(data, "a");
                }
                result.Add(entry);
            }
            return result;
        }

        private JArray BpmEvents(double bpm)
        {
            var result = new JArray();
            if (Tempo.Regions.Count == 0) result.Add(new JObject { ["b"] = 0, ["m"] = bpm });
            else foreach (var region in Tempo.Regions)
                result.Add(new JObject { ["b"] = region.StartBeat, ["m"] = region.Bpm });
            return result;
        }

        private JArray Groups(int kind)
        {
            var result = new JArray();
            foreach (JObject group in V4Documents.Objects(Lightshow, "eventBoxGroups"))
            {
                if (V4Documents.Index(group["t"], "eventBoxGroups.t") != kind) continue;
                var boxes = new JArray();
                foreach (JObject box in V4Documents.Objects(group, "e"))
                {
                    JObject filter = V4Documents.Object(box["filter"], "eventBox.filter");
                    var projected = new JObject
                    {
                        ["f"] = filter.DeepClone(), ["w"] = Number(box, "w"),
                        ["d"] = Number(box, "d", 1), ["t"] = Number(box, "t", 1),
                        ["b"] = Number(box, "b"), ["i"] = Number(box, "e"),
                        ["customData"] = MergeCustomData(box["customData"], box["referenceCustomData"])
                    };
                    if (projected["f"]!["f"] == null) projected["f"]!["f"] = 1;
                    var events = new JArray();
                    foreach (JObject entry in V4Documents.Objects(box, "events"))
                    {
                        JObject data = V4Documents.Object(entry["data"], "event.data");
                        var value = new JObject { ["b"] = Number(entry, "b"), ["customData"] = Metadata(entry) };
                        if (kind == 1)
                        {
                            value["c"] = Number(data, "c"); value["f"] = Number(data, "f");
                            value["s"] = Number(data, "b"); value["sb"] = Number(data, "sb"); value["sf"] = Number(data, "sf");
                            value["i"] = Number(data, "p") != 0 ? 2 : Number(data, "e") == -1 ? 0 : 1;
                        }
                        else
                        {
                            value["e"] = Number(data, "e"); value["p"] = Number(data, "p");
                            if (kind == 2)
                            {
                                value["l"] = Number(data, "l"); value["o"] = Number(data, "d"); value["r"] = Number(data, "r");
                            }
                            else value["t"] = Number(data, "t");
                        }
                        events.Add(value);
                    }
                    if (kind == 1) { projected["r"] = Number(box, "s"); projected["e"] = events; }
                    else
                    {
                        projected["s"] = Number(box, "s"); projected["a"] = Number(box, "a");
                        projected["r"] = Number(box, "f"); projected["l"] = events;
                    }
                    boxes.Add(projected);
                }
                result.Add(new JObject
                {
                    ["b"] = Number(group, "b"), ["g"] = Number(group, "g"), ["e"] = boxes,
                    ["customData"] = group["customData"]?.DeepClone() ?? new JObject()
                });
            }
            return result;
        }

        private static double Number(JObject value, string name, double fallback = 0) => V4Documents.Number(value[name], fallback, name);
    }
}
