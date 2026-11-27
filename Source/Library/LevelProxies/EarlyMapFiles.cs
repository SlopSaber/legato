#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
#nullable enable
#if LEGATO_EARLY_STANDARD_MAPS
global using EarlyLevelInfo = global::UnofficialStandardLevelInfoSaveData;
global using EarlyLevelDifficulty = global::UnofficialStandardLevelInfoSaveData.DifficultyLevel;
#else
global using EarlyLevelInfo = global::CustomLevelInfo;
global using EarlyLevelDifficulty = global::CustomLevelInfo.DifficultyLevelInfo;
#endif
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

    internal static string DefaultCustomLevelsDirectory =>
#if LEGATO_EARLY_STANDARD_MAPS
        Path.Combine(UnityEngine.Application.dataPath, "CustomLevels");
#else
        CustomLevelsModelSO.customLevelsDirectoryPath;
#endif

    internal static EarlyMapEntry? ReadLocal(string root, string? levelId)
    {
        if (string.IsNullOrEmpty(levelId) || !Path.IsPathRooted(levelId)) return null;
        string directory = Path.GetFullPath(levelId);
        if (!string.Equals(Path.GetDirectoryName(directory), root, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return null;

        if (LocalAsset(directory, InfoFileName) is string nativeInfo)
        {
#if LEGATO_EARLY_STANDARD_MAPS
            EarlyMapEntry? stock = ReadNativeInfo(directory, nativeInfo);
            if (stock != null) return stock;
#else
            CustomLevelInfoWrapper? native = CustomLevelInfoWrapperLoader.LoadCustomLevelInfoWrapper(directory);
            if (native?.customLevelInfo.difficultyLevels is { Length: > 0 } difficulties)
                return new EarlyMapEntry(native, nativeInfo, difficulties.Select(chart => chart?.jsonPath).ToArray());
#endif
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
        string? info = LocalAsset(entry.DirectoryPath, Path.GetFileName(entry.InfoPath));
        if (info == null) return null;
        using var hash = SHA1.Create();
        byte[] buffer = new byte[8192];
        AppendFile(info);
        foreach (string? chart in entry.ChartNames)
        {
            string? path = LocalAsset(entry.DirectoryPath, chart);
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

#if LEGATO_EARLY_STANDARD_MAPS
    private static EarlyMapEntry? ReadNativeInfo(string directory, string infoPath)
    {
        try
        {
            string json = File.ReadAllText(infoPath);
            EarlyLevelInfo? info = EarlyLevelInfo.DeserializeFromJSONString(json);
            if (info?.difficultyLevels is not { Length: > 0 } difficulties ||
                info.beatsPerMinute <= 0 || float.IsNaN(info.beatsPerMinute) || float.IsInfinity(info.beatsPerMinute)) return null;

            using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
            JObject document = JObject.Load(reader);
            JArray? charts = document["difficultyLevels"] as JArray;
            var speed = new Dictionary<EarlyLevelDifficulty, float>();
            var metadata = new Dictionary<EarlyLevelDifficulty, string>();
            for (int index = 0; index < difficulties.Length; index++)
            {
                EarlyLevelDifficulty? difficulty = difficulties[index];
                if (difficulty == null || charts == null || index >= charts.Count || charts[index] is not JObject chart) continue;
                speed.Add(difficulty, Number(chart, "noteJumpMovementSpeed"));
                if ((chart["_customData"] ?? chart["customData"]) is JObject data)
                    metadata.Add(difficulty, data.ToString(Formatting.None));
            }

            return new EarlyMapEntry(directory, info, infoPath, difficulties.Select(chart => chart?.jsonPath).ToArray())
            {
                JumpSpeeds = speed,
                CustomData = ((document["_customData"] ?? document["customData"]) as JObject)?.ToString(Formatting.None),
                DifficultyData = metadata,
            };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or FormatException or OverflowException)
        {
            return null;
        }
    }
#endif

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

            var metadata = new Dictionary<EarlyLevelDifficulty, string>();
            var difficulties = new List<EarlyLevelDifficulty>();
#if LEGATO_EARLY_STANDARD_MAPS
            var speed = new Dictionary<EarlyLevelDifficulty, float>();
#endif
            var used = new HashSet<LevelDifficulty>();
            foreach (JObject chart in standardCharts)
            {
                string? label = Text(chart, "_difficulty");
                string? filename = Text(chart, "_beatmapFilename");
                if (label == null || filename == null || !label.LevelDifficultyFromName(out LevelDifficulty difficulty) ||
                    LocalAsset(directory, filename) == null || !used.Add(difficulty)) continue;

                var nativeChart = new EarlyLevelDifficulty
                {
                    difficulty = label,
                    difficultyRank = Integer(chart, "_difficultyRank", 2 * (int)difficulty + 1),
#if !LEGATO_EARLY_STANDARD_MAPS
                    noteJumpMovementSpeed = Number(chart, "_noteJumpMovementSpeed"),
#endif
                    audioPath = audio,
                    jsonPath = filename,
                };
#if LEGATO_EARLY_STANDARD_MAPS
                speed.Add(nativeChart, Number(chart, "_noteJumpMovementSpeed"));
#endif
                difficulties.Add(nativeChart);
                if (chart["_customData"] is JObject data) metadata.Add(nativeChart, data.ToString(Formatting.None));
            }
            if (difficulties.Count == 0) return null;

            var nativeInfo = new EarlyLevelInfo
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
#if LEGATO_EARLY_STANDARD_MAPS
            return new EarlyMapEntry(directory, nativeInfo, infoPath,
#else
            return new EarlyMapEntry(new CustomLevelInfoWrapper(directory, nativeInfo), infoPath,
#endif
                allCharts.Select(chart => Text(chart, "_beatmapFilename")).ToArray())
            {
#if LEGATO_EARLY_STANDARD_MAPS
                JumpSpeeds = speed,
#endif
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
#if LEGATO_EARLY_STANDARD_MAPS
    internal EarlyMapEntry(string directory, EarlyLevelInfo info, string infoPath, IReadOnlyList<string?> chartNames)
    {
        DirectoryPath = directory;
        LevelId = directory;
        Info = info;
        InfoPath = infoPath;
        ChartNames = chartNames;
    }

    internal IReadOnlyDictionary<EarlyLevelDifficulty, float>? JumpSpeeds { get; set; }
#else
    internal EarlyMapEntry(CustomLevelInfoWrapper native, string infoPath, IReadOnlyList<string?> chartNames)
    {
        Native = native;
        DirectoryPath = native.path;
        LevelId = native.levelID;
        Info = native.customLevelInfo;
        InfoPath = infoPath;
        ChartNames = chartNames;
    }

    internal CustomLevelInfoWrapper Native { get; }
#endif
    internal string DirectoryPath { get; }
    internal string LevelId { get; }
    internal EarlyLevelInfo Info { get; }
    internal string InfoPath { get; }
    internal IReadOnlyList<string?> ChartNames { get; }
    internal float SongTimeOffset { get; set; }
    internal float Shuffle { get; set; }
    internal float ShufflePeriod { get; set; } = 1f;
    internal string? CustomData { get; set; }
    internal IReadOnlyDictionary<EarlyLevelDifficulty, string>? DifficultyData { get; set; }

    internal float JumpSpeedFor(EarlyLevelDifficulty difficulty) =>
#if LEGATO_EARLY_STANDARD_MAPS
        JumpSpeeds != null && JumpSpeeds.TryGetValue(difficulty, out float speed) ? speed : 0f;
#else
        difficulty.noteJumpMovementSpeed;
#endif
}
#endif
