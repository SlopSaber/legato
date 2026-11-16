#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Legato.Beatmaps.Formats
{
    internal static class V4Documents
    {
        internal static JObject Read(string path)
        {
            using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            int first = file.ReadByte();
            int second = file.ReadByte();
            file.Position = 0;
            if (first == 0x1f && second == 0x8b)
            {
                using var gzip = new GZipStream(file, CompressionMode.Decompress);
                return Read(gzip);
            }
            return Read(file);
        }

        private static JObject Read(Stream stream)
        {
            using var text = new StreamReader(stream);
            using var json = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None };
            JObject result = JObject.Load(json);
            while (json.Read())
            {
                if (json.TokenType != JsonToken.Comment)
                    throw new InvalidDataException("Unexpected data after the map document.");
            }
            return result;
        }

        internal static JObject Object(JToken? token, string location) => token as JObject
            ?? throw new InvalidDataException("Expected an object at " + location + ".");

        internal static IEnumerable<JObject> Objects(JObject document, string name)
        {
            JToken? token = document[name];
            if (token == null || token.Type == JTokenType.Null) yield break;
            if (token is not JArray array) throw new InvalidDataException("Expected an array at " + name + ".");
            for (int i = 0; i < array.Count; i++) yield return Object(array[i], name + "[" + i + "]");
        }

        internal static JObject Indexed(JObject document, string name, JToken? index)
        {
            if (document[name] == null && index == null) return new JObject();
            int position = Index(index, name);
            if (document[name] is not JArray array || position >= array.Count)
                throw new InvalidDataException("Metadata index is outside " + name + ": " + position + ".");
            return Object(array[position], name + "[" + position + "]");
        }

        internal static int Index(JToken? token, string location)
        {
            double value = Number(token, 0, location);
            if (value < 0 || value > int.MaxValue || Math.Truncate(value) != value)
                throw new InvalidDataException("Invalid index at " + location + ".");
            return (int)value;
        }

        internal static double Number(JToken? token, double fallback, string location)
        {
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
                throw new InvalidDataException("Expected a number at " + location + ".");
            double value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException("Non-finite number at " + location + ".");
            return value;
        }

        internal static JObject ExpandGameplay(JObject source)
        {
            JObject expanded = CopyExcept(source, "colorNotes", "bombNotes", "obstacles", "arcs", "chains", "njsEvents",
                "colorNotesData", "bombNotesData", "obstaclesData", "arcsData", "chainsData", "njsEventData");
            ExpandItems(source, expanded, "colorNotes", "colorNotesData");
            ExpandItems(source, expanded, "bombNotes", "bombNotesData");
            ExpandItems(source, expanded, "obstacles", "obstaclesData");
            ExpandItems(source, expanded, "njsEvents", "njsEventData");
            JArray arcs = new();
            foreach (JObject item in Objects(source, "arcs"))
            {
                JObject arc = (JObject)item.DeepClone();
                arc["data"] = Indexed(source, "arcsData", item["ai"]).DeepClone();
                arc["headData"] = Indexed(source, "colorNotesData", item["hi"]).DeepClone();
                arc["tailData"] = Indexed(source, "colorNotesData", item["ti"]).DeepClone();
                arcs.Add(arc);
            }
            expanded["arcs"] = arcs;
            JArray chains = new();
            foreach (JObject item in Objects(source, "chains"))
            {
                JObject chain = (JObject)item.DeepClone();
                chain["data"] = Indexed(source, "chainsData", item["ci"]).DeepClone();
                chain["headData"] = Indexed(source, "colorNotesData", item["i"]).DeepClone();
                chains.Add(chain);
            }
            expanded["chains"] = chains;
            return expanded;
        }

        internal static JObject ExpandLightshow(JObject source)
        {
            JObject expanded = CopyExcept(source, "waypoints", "basicEvents", "colorBoostEvents", "eventBoxGroups",
                "waypointsData", "basicEventsData", "colorBoostEventsData", "indexFilters", "lightColorEventBoxes",
                "lightColorEvents", "lightRotationEventBoxes", "lightRotationEvents", "lightTranslationEventBoxes",
                "lightTranslationEvents", "fxEventBoxes", "floatFxEvents");
            ExpandItems(source, expanded, "waypoints", "waypointsData");
            ExpandItems(source, expanded, "basicEvents", "basicEventsData");
            ExpandItems(source, expanded, "colorBoostEvents", "colorBoostEventsData");
            JArray groups = new();
            foreach (JObject item in Objects(source, "eventBoxGroups"))
            {
                int kind = Index(item["t"], "eventBoxGroups.t");
                string boxes;
                string events;
                switch (kind)
                {
                    case 1: boxes = "lightColorEventBoxes"; events = "lightColorEvents"; break;
                    case 2: boxes = "lightRotationEventBoxes"; events = "lightRotationEvents"; break;
                    case 3: boxes = "lightTranslationEventBoxes"; events = "lightTranslationEvents"; break;
                    case 4: boxes = "fxEventBoxes"; events = "floatFxEvents"; break;
                    case 0: groups.Add(item.DeepClone()); continue;
                    default: throw new InvalidDataException("Unknown v4 event box type: " + kind + ".");
                }
                JObject group = (JObject)item.DeepClone();
                JArray expandedBoxes = new();
                foreach (JObject reference in Objects(item, "e"))
                {
                    JObject box = (JObject)Indexed(source, boxes, reference["e"]).DeepClone();
                    box["filter"] = Indexed(source, "indexFilters", reference["f"]).DeepClone();
                    JArray expandedEvents = new();
                    foreach (JObject eventReference in Objects(reference, "l"))
                    {
                        JObject entry = (JObject)eventReference.DeepClone();
                        entry["data"] = Indexed(source, events, eventReference["i"]).DeepClone();
                        expandedEvents.Add(entry);
                    }
                    box["events"] = expandedEvents;
                    box["referenceCustomData"] = reference["customData"]?.DeepClone();
                    expandedBoxes.Add(box);
                }
                group["e"] = expandedBoxes;
                groups.Add(group);
            }
            expanded["eventBoxGroups"] = groups;
            return expanded;
        }

        private static void ExpandItems(JObject source, JObject target, string name, string dataName)
        {
            JArray items = new();
            foreach (JObject item in Objects(source, name))
            {
                JObject entry = (JObject)item.DeepClone();
                entry["data"] = Indexed(source, dataName, item["i"]).DeepClone();
                items.Add(entry);
            }
            target[name] = items;
        }

        private static JObject CopyExcept(JObject source, params string[] names)
        {
            HashSet<string> omitted = new(names, StringComparer.Ordinal);
            JObject target = new();
            foreach (JProperty property in source.Properties())
                if (!omitted.Contains(property.Name)) target.Add(property.Name, property.Value.DeepClone());
            return target;
        }
    }
}
