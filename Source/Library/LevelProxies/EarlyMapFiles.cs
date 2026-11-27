#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Legato.Beatmaps;

internal static class EarlyMapFiles
{
    internal const string InfoFileName = "info.json";
    private const string StandardInfoFileName = "Info.dat";

    internal static EarlyMapEntry? ReadLocal(string root, string? levelId)
    {
        if (string.IsNullOrEmpty(levelId) || !Path.IsPathRooted(levelId)) return null;
        string directory = Path.GetFullPath(levelId);
        if (!string.Equals(Path.GetDirectoryName(directory), root, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return null;

        if (LocalAsset(directory, InfoFileName) is string nativeInfo)
        {
            CustomLevelInfoWrapper? native = CustomLevelInfoWrapperLoader.LoadCustomLevelInfoWrapper(directory);
            if (native?.customLevelInfo.difficultyLevels is { Length: > 0 } difficulties)
                return new EarlyMapEntry(native, nativeInfo, difficulties.Select(chart => chart?.jsonPath).ToArray());
        }

        return LocalAsset(directory, StandardInfoFileName) is string standardInfo
            ? ReadStandardInfo(directory, standardInfo)
            : null;
    }

    internal static string? FindHash(string root, string levelId)
    {
        try
        {
            EarlyMapEntry? entry = ReadLocal(root, levelId);
            return entry == null ? null : ComputeHash(entry);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    internal static string? LocalAsset(string directory, string? name)
    {
        if (string.IsNullOrEmpty(name) || Path.IsPathRooted(name)) return null;
        string path = Path.GetFullPath(Path.Combine(directory, name));
        if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(path)) return null;

        for (string? item = path; item != null && !string.Equals(item, directory, StringComparison.OrdinalIgnoreCase);
             item = Path.GetDirectoryName(item))
        {
            if ((File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0) return null;
        }
        return path;
    }

    internal static string? ComputeHash(EarlyMapEntry entry)
    {
        string? info = LocalAsset(entry.Native.path, Path.GetFileName(entry.InfoPath));
        if (info == null) return null;
        using var hash = SHA1.Create();
        byte[] buffer = new byte[8192];
        AppendFile(info);
        foreach (string? chart in entry.ChartNames)
        {
            string? path = LocalAsset(entry.Native.path, chart);
            if (path == null) return null;
            AppendFile(path);
        }
        hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return BitConverter.ToString(hash.Hash).Replace("-", string.Empty);

        void AppendFile(string path)
        {
            using Stream input = File.OpenRead(path);
            int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
                hash.TransformBlock(buffer, 0, count, buffer, 0);
        }
    }

    private static EarlyMapEntry? ReadStandardInfo(string directory, string infoPath)
    {
        try
        {
            using StreamReader input = File.OpenText(infoPath);
            using var reader = new JsonTextReader(input) { DateParseHandling = DateParseHandling.None };
            JObject info = JObject.Load(reader);
            if (!Version.TryParse(Text(info, "_version"), out Version version) || version.Major is < 1 or > 2) return null;

            string? audio = Text(info, "_songFilename") ?? Text(info, "_songFileName");
            float tempo = Number(info, "_beatsPerMinute");
            if (audio == null || LocalAsset(directory, audio) == null || tempo <= 0) return null;

            IEnumerable<JObject> allCharts;
            IEnumerable<JObject> standardCharts;
            if (info["_difficultyBeatmapSets"] is JArray sets && version.Major == 2)
            {
                allCharts = sets.OfType<JObject>().SelectMany(ChartsIn);
                standardCharts = sets.OfType<JObject>()
                    .Where(set => string.Equals(Text(set, "_beatmapCharacteristicName"), "Standard", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(ChartsIn);
            }
            else if (version.Major == 1 && info["_difficultyBeatmaps"] is JArray flatCharts)
            {
                allCharts = standardCharts = flatCharts.OfType<JObject>();
            }
            else return null;

            var metadata = new Dictionary<CustomLevelInfo.DifficultyLevelInfo, string>();
            var difficulties = new List<CustomLevelInfo.DifficultyLevelInfo>();
            var used = new HashSet<LevelDifficulty>();
            foreach (JObject chart in standardCharts)
            {
                string? label = Text(chart, "_difficulty");
                string? filename = Text(chart, "_beatmapFilename");
                if (label == null || filename == null || !label.LevelDifficultyFromName(out LevelDifficulty difficulty) ||
                    LocalAsset(directory, filename) == null || !used.Add(difficulty)) continue;

                var nativeChart = new CustomLevelInfo.DifficultyLevelInfo
                {
                    difficulty = label,
                    difficultyRank = Integer(chart, "_difficultyRank", 2 * (int)difficulty + 1),
                    noteJumpMovementSpeed = Number(chart, "_noteJumpMovementSpeed"),
                    audioPath = audio,
                    jsonPath = filename,
                };
                difficulties.Add(nativeChart);
                if (chart["_customData"] is JObject data) metadata.Add(nativeChart, data.ToString(Formatting.None));
            }
            if (difficulties.Count == 0) return null;

            var nativeInfo = new CustomLevelInfo
            {
                songName = Text(info, "_songName") ?? Path.GetFileName(directory),
                songSubName = Text(info, "_songSubName") ?? string.Empty,
                authorName = Text(info, "_songAuthorName") ?? Text(info, "_authorName") ?? string.Empty,
                beatsPerMinute = tempo,
                previewStartTime = Number(info, "_previewStartTime"),
                previewDuration = Number(info, "_previewDuration"),
                coverImagePath = Text(info, "_coverImageFilename") ?? Text(info, "_coverImageFileName") ?? string.Empty,
                environmentName = Text(info, "_environmentName") ?? "DefaultEnvironment",
                difficultyLevels = difficulties.ToArray(),
            };
            return new EarlyMapEntry(new CustomLevelInfoWrapper(directory, nativeInfo), infoPath,
                allCharts.Select(chart => Text(chart, "_beatmapFilename")).ToArray())
            {
                SongTimeOffset = Number(info, "_songTimeOffset"),
                Shuffle = Number(info, "_shuffle"),
                ShufflePeriod = Number(info, "_shufflePeriod", 1f),
                CustomData = (info["_customData"] as JObject)?.ToString(Formatting.None),
                DifficultyData = metadata,
            };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or FormatException or OverflowException)
        {
            return null;
        }

        static IEnumerable<JObject> ChartsIn(JObject set) =>
            (set["_difficultyBeatmaps"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>();
    }

    internal static string? Text(JObject source, string key) =>
        source[key] is JValue { Type: JTokenType.String, Value: string text } ? text : null;

    private static float Number(JObject source, string key, float fallback = 0f)
    {
        if (source[key] is not JValue { Type: JTokenType.Integer or JTokenType.Float } number) return fallback;
        float value = number.Value<float>();
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    }

    private static int Integer(JObject source, string key, int fallback) =>
        source[key] is JValue { Type: JTokenType.Integer } number ? number.Value<int>() : fallback;
}

internal sealed class EarlyMapEntry
{
    internal EarlyMapEntry(CustomLevelInfoWrapper native, string infoPath, IReadOnlyList<string?> chartNames)
    {
        Native = native;
        InfoPath = infoPath;
        ChartNames = chartNames;
    }

    internal CustomLevelInfoWrapper Native { get; }
    internal string InfoPath { get; }
    internal IReadOnlyList<string?> ChartNames { get; }
    internal float SongTimeOffset { get; set; }
    internal float Shuffle { get; set; }
    internal float ShufflePeriod { get; set; } = 1f;
    internal string? CustomData { get; set; }
    internal IReadOnlyDictionary<CustomLevelInfo.DifficultyLevelInfo, string>? DifficultyData { get; set; }
}
#endif
