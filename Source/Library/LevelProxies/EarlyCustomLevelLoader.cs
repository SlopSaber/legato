#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Legato.Beatmaps
{
    internal static class EarlyCustomLevelLoader
    {
        private static readonly MethodInfo LoadChart = typeof(CustomLevelStandardLevelLoader).GetMethod(
            "Load", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(CustomLevelInfo.DifficultyLevelInfo), typeof(string), typeof(BeatmapDataSO) }, null)
            ?? throw new MissingMethodException(typeof(CustomLevelStandardLevelLoader).FullName, "Load");

        internal static async Task<StandardLevelSO?> LoadAsync(CustomLevelInfoWrapper entry, CancellationToken cancellationToken)
        {
            string? audioPath = EarlyLevelCatalog.LocalAsset(entry.path, entry.customLevelInfo.difficultyLevels[0].audioPath);
            if (audioPath == null) return null;

            var completion = new TaskCompletionSource<AudioClip?>();
            using var cancellation = cancellationToken.Register(() => completion.TrySetCanceled());
            SharedCoroutineStarter.instance.StartCoroutine(ReadAudio(audioPath, completion));
            AudioClip? audio = await completion.Task;
            if (!audio) return null;

            StandardLevelSO? level = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                level = BuildLevel(entry, audio!);
                return level;
            }
            finally
            {
                if (!level) Object.Destroy(audio);
            }
        }

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

        private static StandardLevelSO? BuildLevel(CustomLevelInfoWrapper entry, AudioClip audio)
        {
            CustomLevelInfo info = entry.customLevelInfo;
            if (!(info.beatsPerMinute > 0f) || float.IsInfinity(info.beatsPerMinute)) return null;

            SceneInfo environment = Resources.Load<SceneInfo>("SceneInfo/" + info.environmentName + "SceneInfo");
            if (!environment) environment = Resources.Load<SceneInfo>("SceneInfo/DefaultEnvironmentSceneInfo");
            if (!environment) return null;

            var chartLoader = ScriptableObject.CreateInstance<CustomLevelStandardLevelLoader>();
            var difficulties = new List<StandardLevelSO.DifficultyBeatmap>();
            var charts = new List<BeatmapDataSO>();
            StandardLevelSO? level = null;
            bool published = false;
            try
            {
                foreach (CustomLevelInfo.DifficultyLevelInfo difficulty in info.difficultyLevels)
                {
                    if (string.IsNullOrEmpty(difficulty?.difficulty) ||
                        EarlyLevelCatalog.LocalAsset(entry.path, difficulty.jsonPath) == null) continue;

                    var chart = ScriptableObject.CreateInstance<BeatmapDataSO>();
                    charts.Add(chart);
                    var native = (StandardLevelSO.DifficultyBeatmap?)LoadChart.Invoke(chartLoader, new object[] { difficulty, entry.path, chart });
                    if (native != null) difficulties.Add(native);
                    else
                    {
                        charts.RemoveAt(charts.Count - 1);
                        Object.Destroy(chart);
                    }
                }
                if (difficulties.Count == 0) return null;

                level = ScriptableObject.CreateInstance<StandardLevelSO>();
                level.InitFull(entry.levelID, info.songName ?? string.Empty, info.songSubName ?? string.Empty,
                    info.authorName ?? string.Empty, audio, audio.length, info.beatsPerMinute, 0f, 0f, 1f,
                    info.previewStartTime, info.previewDuration, null, difficulties.ToArray(), environment);
                published = true;
                return level;
            }
            catch (TargetInvocationException error) when (error.InnerException is IOException or UnauthorizedAccessException)
            {
                return null;
            }
            finally
            {
                Object.Destroy(chartLoader);
                if (!published)
                {
                    if (level) Object.Destroy(level);
                    foreach (BeatmapDataSO chart in charts) Object.Destroy(chart);
                }
            }
        }
    }
}
#endif
