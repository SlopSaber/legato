#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Legato.Beatmaps
{
    internal static class EarlyCustomLevelLoader
    {
        internal static async Task<StandardLevelSO?> LoadAsync(EarlyMapEntry entry, CancellationToken cancellationToken,
            IReadOnlyCollection<LevelDifficulty>? allowedDifficulties = null, bool longPreviews = false)
        {
            string? audioPath = EarlyMapFiles.LocalAsset(entry.DirectoryPath, entry.Info.difficultyLevels[0]?.audioPath);
            if (audioPath == null || !IsPositive(entry.Info.beatsPerMinute)) return null;

            List<(EarlyLevelDifficulty Difficulty, LevelDifficulty Kind, string Json)> charts =
                await Task.Run(() => ReadCharts(entry, allowedDifficulties, cancellationToken), cancellationToken);
            if (charts.Count == 0) return null;
            byte[]? coverBytes = await Task.Run(() => ReadCover(entry), cancellationToken);
            await UnityGame.SwitchToMainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<AudioClip?>();
            using var cancellation = cancellationToken.Register(() => completion.TrySetCanceled());
            SharedCoroutineStarter.instance.StartCoroutine(ReadAudio(audioPath, completion));
            AudioClip? audio = await completion.Task;
            if (!audio) return null;

            StandardLevelSO? level = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                level = BuildLevel(entry, audio!, charts, coverBytes, longPreviews);
                return level;
            }
            finally
            {
                if (!level) Object.Destroy(audio);
            }
        }

        private static List<(EarlyLevelDifficulty Difficulty, LevelDifficulty Kind, string Json)>
            ReadCharts(EarlyMapEntry entry, IReadOnlyCollection<LevelDifficulty>? allowedDifficulties, CancellationToken cancellationToken)
        {
            var charts = new List<(EarlyLevelDifficulty, LevelDifficulty, string)>();
            foreach (EarlyLevelDifficulty difficulty in entry.Info.difficultyLevels)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (difficulty == null || string.IsNullOrEmpty(difficulty.difficulty) ||
                    !difficulty.difficulty.LevelDifficultyFromName(out LevelDifficulty kind) ||
                    (allowedDifficulties != null && !allowedDifficulties.Contains(kind))) continue;

                try
                {
                    string? path = EarlyMapFiles.LocalAsset(entry.DirectoryPath, difficulty.jsonPath);
                    if (path == null) continue;
                    string json = File.ReadAllText(path);
                    using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
                    JObject chart = JObject.Load(reader);
                    if (EarlyMapFiles.Text(chart, "_version") != "2.0.0" || chart["_notes"] is not JArray notes ||
                        chart["_obstacles"] is not JArray obstacles || chart["_events"] is not JArray events ||
                        (chart["_customData"] as JObject)?["_BPMChanges"] is JArray { Count: > 0 } ||
                        events.Any(item => item is not JObject itemData || itemData.Value<int>("_type") == 100) ||
                        notes.Any(item => item is not JObject itemData || !HasLane(itemData) ||
                            itemData.Value<int>("_lineLayer") is < 0 or > 2 || itemData.Value<int>("_type") is < 0 or > 3 ||
                            itemData.Value<int>("_cutDirection") is < 0 or > 9) ||
                        obstacles.Any(item => item is not JObject itemData || !HasLane(itemData) ||
                            itemData.Value<int>("_type") is < 0 or > 1)) continue;

                    charts.Add((difficulty, kind, json));
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
                    JsonException or FormatException or OverflowException or InvalidCastException)
                {
                    // A bad chart leaves the remaining native difficulties available.
                }
            }
            return charts;

            static bool HasLane(JObject item) => item["_lineIndex"] is JValue { Type: JTokenType.Integer } index &&
                index.Value<int>() is >= 0 and < 4;
        }

        private static byte[]? ReadCover(EarlyMapEntry entry)
        {
            try
            {
                string? coverPath = EarlyMapFiles.LocalAsset(entry.DirectoryPath, entry.Info.coverImagePath);
                return coverPath == null ? null : File.ReadAllBytes(coverPath);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        private static bool IsPositive(float number) => number > 0 && !float.IsInfinity(number);

        private static IEnumerator ReadAudio(string path, TaskCompletionSource<AudioClip?> completion)
        {
            using var request = new WWW(new Uri(path).AbsoluteUri);
            while (!request.isDone && !completion.Task.IsCompleted) yield return null;
            if (completion.Task.IsCompleted) yield break;
            if (!string.IsNullOrEmpty(request.error))
            {
                completion.TrySetResult(null);
                yield break;
            }

            string extension = Path.GetExtension(path).ToLowerInvariant();
            AudioType type = extension == ".egg" ? AudioType.OGGVORBIS : AudioType.UNKNOWN;
            AudioClip audio = request.GetAudioClip(true, true, type);
            if (!audio)
            {
                completion.TrySetResult(null);
                yield break;
            }

            while (audio.loadState != AudioDataLoadState.Loaded && audio.loadState != AudioDataLoadState.Failed &&
                   !completion.Task.IsCompleted) yield return null;
            bool loaded = audio.loadState == AudioDataLoadState.Loaded;
            if (!completion.TrySetResult(loaded ? audio : null) || !loaded) Object.Destroy(audio);
        }

        private static StandardLevelSO? BuildLevel(EarlyMapEntry entry, AudioClip audio,
            List<(EarlyLevelDifficulty Difficulty, LevelDifficulty Kind, string Json)> sources, byte[]? coverBytes, bool longPreviews)
        {
            EarlyLevelInfo info = entry.Info;
            float previewStart = Mathf.Clamp(info.previewStartTime, 0f, Mathf.Max(0f, audio.length - 0.1f));
            float remainingAudio = audio.length - previewStart;
            float previewDuration = longPreviews ? remainingAudio : Mathf.Clamp(info.previewDuration, 0f, remainingAudio);

            SceneInfo environment = UnityEngine.Resources.Load<SceneInfo>("SceneInfo/" + info.environmentName + "SceneInfo");
            if (!environment) environment = UnityEngine.Resources.Load<SceneInfo>("SceneInfo/DefaultEnvironmentSceneInfo");
            if (!environment) return null;

            var difficulties = new List<StandardLevelSO.DifficultyBeatmap>();
            var charts = new List<BeatmapDataSO>();
            var level = ScriptableObject.CreateInstance<StandardLevelSO>();
            Sprite? cover = null;
            Texture2D? texture = null;
            bool published = false;
            try
            {
                if (coverBytes != null)
                {
                    texture = new Texture2D(2, 2);
                    if (texture.LoadImage(coverBytes, true))
                        cover = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100,
                            0, SpriteMeshType.FullRect);
                    else
                    {
                        Object.Destroy(texture);
                        texture = null;
                    }
                }

                // The native difficulty constructor reads tempo and shuffle from its parent immediately.
                Initialize(Array.Empty<StandardLevelSO.DifficultyBeatmap>());
                foreach ((EarlyLevelDifficulty difficulty, LevelDifficulty kind, string json) in sources)
                {
                    var chart = ScriptableObject.CreateInstance<BeatmapDataSO>();
                    charts.Add(chart);
                    chart.SetJsonData(json);
                    string? difficultyData = entry.DifficultyData != null && entry.DifficultyData.TryGetValue(difficulty, out string metadata)
                        ? metadata : null;
#if LEGATO_EARLY_STANDARD_MAPS
                    EarlyBeatmapSources.AssociateMetadata(chart, entry.CustomData, difficultyData);
#else
                    EarlyBeatmapSources.Associate(chart, difficulty, entry.CustomData, difficultyData);
#endif
                    difficulties.Add(new StandardLevelSO.DifficultyBeatmap(level, kind, difficulty.difficultyRank,
                        entry.JumpSpeedFor(difficulty), chart));
                }
                Initialize(difficulties.ToArray());
                published = true;
                return level;
            }
            finally
            {
                if (!published)
                {
                    if (level) Object.Destroy(level);
                    foreach (BeatmapDataSO chart in charts) Object.Destroy(chart);
                    if (cover) Object.Destroy(cover);
                    if (texture) Object.Destroy(texture);
                }
            }

            void Initialize(StandardLevelSO.DifficultyBeatmap[] beatmaps) =>
                level.InitFull(entry.LevelId, info.songName ?? string.Empty, info.songSubName ?? string.Empty,
                    info.authorName ?? string.Empty, audio, audio.length, info.beatsPerMinute, entry.SongTimeOffset, entry.Shuffle, entry.ShufflePeriod,
                    previewStart, previewDuration, cover, beatmaps, environment);
        }
    }
}
#endif
