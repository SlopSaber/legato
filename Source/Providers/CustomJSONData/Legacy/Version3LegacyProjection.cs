#if V1_11_1
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CustomJSONData.HarmonyPatches
{
    internal static class Version3LegacyProjection
    {
        internal static JObject Convert(JObject source, Version version)
        {
            JObject customData = source["customData"] is JObject data ? (JObject)data.DeepClone() : new JObject();
            var notes = Objects(source, "colorNotes").Select(item => Note(item, bomb: false, fake: false))
                .Concat(Objects(source, "bombNotes").Select(item => Note(item, bomb: true, fake: false)))
                .Concat(Objects(customData, "fakeColorNotes").Select(item => Note(item, bomb: false, fake: true)))
                .Concat(Objects(customData, "fakeBombNotes").Select(item => Note(item, bomb: true, fake: true)));
            var obstacles = Objects(source, "obstacles").Select(item => Obstacle(item, fake: false))
                .Concat(Objects(customData, "fakeObstacles").Select(item => Obstacle(item, fake: true)));
            var events = Objects(source, "basicBeatmapEvents")
                .Where(item => item.Value<int>("et") is >= 0 and <= 15)
                .Select(item => Event(item, item.Value<int>("et"), item.Value<int>("i")))
                .Concat(Objects(source, "bpmEvents").Where(item => item.Value<float>("m") > 0)
                    .Select(item => Event(item, 10, Math.Max(1, (int)Math.Round(item.Value<float>("m"))))))
                .Concat(RotationEvents(source));

            if (customData["customEvents"] is JArray)
            {
                customData["_customEvents"] = new JArray(Objects(customData, "customEvents").Select(item => new JObject
                {
                    ["_time"] = item.Value<float>("b"),
                    ["_type"] = item.Value<string>("t"),
                    ["_data"] = item["d"]?.DeepClone()
                }));
                customData.Remove("customEvents");
            }

            return new JObject
            {
                ["_version"] = version.ToString(),
                ["_notes"] = new JArray(notes.OrderBy(item => item.Value<float>("_time"))),
                ["_obstacles"] = new JArray(obstacles.OrderBy(item => item.Value<float>("_time"))),
                ["_events"] = new JArray(events.OrderBy(item => item.Value<float>("_time"))),
                ["_customData"] = customData
            };
        }

        private static JObject Note(JObject source, bool bomb, bool fake)
        {
            JObject data = ObjectData(source, fake);
            int angle = source.Value<int>("a");
            if (!bomb && angle != 0)
            {
                data["cutDirectionAngleOffset"] = angle;
            }

            return new JObject
            {
                ["_time"] = source.Value<float>("b"),
                ["_lineIndex"] = source.Value<int>("x"),
                ["_lineLayer"] = source.Value<int>("y"),
                ["_type"] = bomb ? 3 : source.Value<int>("c"),
                ["_cutDirection"] = bomb ? 8 : source.Value<int>("d"),
                ["_customData"] = data
            };
        }

        private static JObject Obstacle(JObject source, bool fake)
        {
            JObject data = ObjectData(source, fake);
            int x = source.Value<int>("x");
            int y = source.Value<int>("y");
            int width = source.Value<int>("w");
            int height = source.Value<int>("h");
            data["coordinates"] ??= new JArray(x - 2, y);
            data["size"] ??= new JArray(width, height);

            return new JObject
            {
                ["_time"] = source.Value<float>("b"),
                ["_lineIndex"] = x,
                ["_type"] = y >= 2 ? 1 : 0,
                ["_duration"] = source.Value<float>("d"),
                ["_width"] = Math.Max(1, width),
                ["_customData"] = data
            };
        }

        private static JObject Event(JObject source, int type, int value) => new()
        {
            ["_time"] = source.Value<float>("b"),
            ["_type"] = type,
            ["_value"] = value,
            ["_floatValue"] = source.Value<float?>("f") ?? 1,
            ["_customData"] = source["customData"]?.DeepClone()
        };

        private static IEnumerable<JObject> RotationEvents(JObject source)
        {
            // These are the rotation steps accepted by the 1.11.1 event processor.
            float[] rotations = { -60, -45, -30, -15, 15, 30, 45, 60 };
            foreach (JObject item in Objects(source, "rotationEvents"))
            {
                int value = Array.IndexOf(rotations, item.Value<float>("r"));
                if (value >= 0)
                {
                    yield return Event(item, item.Value<int>("e") == 0 ? 14 : 15, value);
                }
            }
        }

        private static JObject ObjectData(JObject source, bool fake)
        {
            JObject data = source["customData"] is JObject custom ? (JObject)custom.DeepClone() : new JObject();
            if (fake)
            {
                data["NE_fake"] = true;
            }

            return data;
        }

        private static IEnumerable<JObject> Objects(JObject source, string name) =>
            source[name] is JArray list ? list.OfType<JObject>() : Enumerable.Empty<JObject>();
    }
}
#endif
