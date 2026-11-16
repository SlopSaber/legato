#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Legato.Beatmaps.Formats
{
    public sealed class V4LevelInfo
    {
        public JObject Document { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Author { get; }
        public string AudioFilename { get; }
        public string PreviewFilename { get; }
        public string AudioDataFilename { get; }
        public string CoverFilename { get; }
        public double Bpm { get; }
        public double SongDuration { get; }
        public double PreviewStartTime { get; }
        public double PreviewDuration { get; }
        public IReadOnlyList<Difficulty> Difficulties { get; }

        public V4LevelInfo(JObject document)
        {
            Document = document;
            string version = Text(document, "version");
            if (!Version.TryParse(version, out Version? parsed) || parsed.Major != 4)
                throw new InvalidDataException("Expected v4 level metadata.");
            JObject song = V4Documents.Object(document["song"], "song");
            JObject audio = V4Documents.Object(document["audio"], "audio");
            Title = Text(song, "title"); Subtitle = Text(song, "subTitle"); Author = Text(song, "author");
            AudioFilename = Text(audio, "songFilename");
            PreviewFilename = Text(document, "songPreviewFilename");
            if (PreviewFilename.Length == 0) PreviewFilename = AudioFilename;
            AudioDataFilename = Text(audio, "audioDataFilename");
            CoverFilename = Text(document, "coverImageFilename");
            Bpm = V4Documents.Number(audio["bpm"], 120, "audio.bpm");
            if (Bpm <= 0) throw new InvalidDataException("The initial BPM must be positive.");
            SongDuration = V4Documents.Number(audio["songDuration"], 0, "audio.songDuration");
            PreviewStartTime = V4Documents.Number(audio["previewStartTime"], 0, "audio.previewStartTime");
            PreviewDuration = V4Documents.Number(audio["previewDuration"], 0, "audio.previewDuration");
            Difficulties = V4Documents.Objects(document, "difficultyBeatmaps").Select(value => new Difficulty(value)).ToArray();
        }

        public string ResolveAsset(string directory, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || Path.IsPathRooted(filename))
                throw new InvalidDataException("A map asset must use a relative filename.");
            string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, filename));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A map asset leaves its level directory.");
            return path;
        }

        internal JObject LegacyMetadata(bool previewAudio)
        {
            JArray sets = new();
            foreach (IGrouping<string, Difficulty> group in Difficulties.GroupBy(value => value.Characteristic))
            {
                JArray difficulties = new();
                foreach (Difficulty value in group)
                {
                    JObject custom = LegacyCustomData(value.Document["customData"]);
                    custom["_environmentNameIdx"] = value.EnvironmentIndex;
                    custom["_colorSchemeIdx"] = value.ColorSchemeIndex;
                    difficulties.Add(new JObject
                    {
                        ["_difficulty"] = value.Name,
                        ["_difficultyRank"] = DifficultyRank(value.Name),
                        ["_beatmapFilename"] = value.BeatmapFilename,
                        ["_noteJumpMovementSpeed"] = value.NoteJumpSpeed,
                        ["_noteJumpStartBeatOffset"] = value.NoteJumpOffset,
                        ["_customData"] = custom
                    });
                }
                sets.Add(new JObject { ["_beatmapCharacteristicName"] = group.Key, ["_difficultyBeatmaps"] = difficulties });
            }
            string[] mappers = Difficulties.SelectMany(value => value.Mappers).Distinct(StringComparer.Ordinal).ToArray();
            JArray environments = Document["environmentNames"] as JArray ?? new JArray();
            string environment = environments.Count == 0 ? "DefaultEnvironment" : environments[0]?.Value<string>() ?? "DefaultEnvironment";
            return new JObject
            {
                ["_version"] = "2.1.0",
                ["_songName"] = Title,
                ["_songSubName"] = Subtitle,
                ["_songAuthorName"] = Author,
                ["_levelAuthorName"] = string.Join(", ", mappers),
                ["_beatsPerMinute"] = Bpm,
                ["_songTimeOffset"] = 0,
                ["_shuffle"] = 0,
                ["_shufflePeriod"] = 0,
                ["_previewStartTime"] = PreviewStartTime,
                ["_previewDuration"] = PreviewDuration,
                ["_songFilename"] = previewAudio ? PreviewFilename : AudioFilename,
                ["_coverImageFilename"] = CoverFilename,
                ["_environmentName"] = environment,
                ["_allDirectionsEnvironmentName"] = environment,
                ["_environmentNames"] = environments.DeepClone(),
                ["_difficultyBeatmapSets"] = sets,
                ["_customData"] = LegacyCustomData(Document["customData"])
            };
        }

        private static JObject LegacyCustomData(JToken? token)
        {
            JObject result = token is JObject source ? (JObject)source.DeepClone() : new JObject();
            foreach (JProperty property in result.Properties().ToArray())
                if (!property.Name.StartsWith("_", StringComparison.Ordinal) && result["_" + property.Name] == null)
                    result["_" + property.Name] = property.Value.DeepClone();
            return result;
        }

        private static int DifficultyRank(string name)
        {
            switch (name)
            {
                case "Easy": return 1;
                case "Normal": return 3;
                case "Hard": return 5;
                case "Expert": return 7;
                case "ExpertPlus": return 9;
                default: throw new InvalidDataException("Unknown difficulty: " + name + ".");
            }
        }

        private static string Text(JObject value, string name) => value[name]?.Value<string>() ?? string.Empty;

        public sealed class Difficulty
        {
            public JObject Document { get; }
            public string Characteristic { get; }
            public string Name { get; }
            public string BeatmapFilename { get; }
            public string LightshowFilename { get; }
            public int EnvironmentIndex { get; }
            public int ColorSchemeIndex { get; }
            public double NoteJumpSpeed { get; }
            public double NoteJumpOffset { get; }
            public string[] Mappers { get; }
            public string[] Lighters { get; }

            internal Difficulty(JObject document)
            {
                Document = document;
                Characteristic = Text(document, "characteristic"); Name = Text(document, "difficulty");
                BeatmapFilename = Text(document, "beatmapDataFilename"); LightshowFilename = Text(document, "lightshowDataFilename");
                EnvironmentIndex = SignedIndex(document["environmentNameIdx"], 0);
                ColorSchemeIndex = SignedIndex(document["beatmapColorSchemeIdx"], -1);
                NoteJumpSpeed = V4Documents.Number(document["noteJumpMovementSpeed"], 0, "noteJumpMovementSpeed");
                NoteJumpOffset = V4Documents.Number(document["noteJumpStartBeatOffset"], 0, "noteJumpStartBeatOffset");
                JObject authors = document["beatmapAuthors"] as JObject ?? new JObject();
                Mappers = Strings(authors["mappers"]); Lighters = Strings(authors["lighters"]);
            }

            private static string[] Strings(JToken? token) => token is JArray array
                ? array.Select(value => value.Value<string>() ?? string.Empty).ToArray() : Array.Empty<string>();

            private static int SignedIndex(JToken? token, int fallback)
            {
                double value = V4Documents.Number(token, fallback, "metadata index");
                if (value < -1 || value > int.MaxValue || Math.Truncate(value) != value)
                    throw new InvalidDataException("Invalid metadata index.");
                return (int)value;
            }
        }
    }
}
