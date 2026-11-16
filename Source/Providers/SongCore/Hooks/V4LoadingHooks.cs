#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BeatmapSaveDataVersion3;
using CustomJSONData.CustomBeatmap;
using IPA.Utilities;
using Legato.Beatmaps.Formats;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

namespace SongCore.Hooks
{
    internal sealed class V4LoadingHooks : IInitializable, IDisposable
    {
        private const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static readonly ConditionalWeakTable<CustomPreviewBeatmapLevel, LevelContext> Levels = new();
        private static readonly ConditionalWeakTable<StandardLevelInfoSaveData, LevelContext> Metadata = new();
        private static readonly ConditionalWeakTable<BeatmapSaveData, V4LegacyProjection> Projections = new();
        private static readonly ConditionalWeakTable<BeatmapSaveData, V4RelativeNjs> RelativeSpeeds = new();
        private static readonly AsyncLocal<DifficultyContext?> Loading = new();
        private static readonly AsyncLocal<V4LegacyProjection?> Converting = new();
        private readonly List<Hook> _hooks = new();
        private readonly EnvironmentsListSO _environments;
        private V4LoadingHooks(EnvironmentsListSO environments) => _environments = environments;

        public void Initialize()
        {
            _hooks.Add(new Hook(typeof(BeatmapEnvironmentHelper).GetMethod(nameof(BeatmapEnvironmentHelper.GetEnvironmentInfo))!, EnvironmentForDifficulty, true));
            _hooks.Add(new Hook(typeof(CustomLevelLoader).GetMethod(nameof(CustomLevelLoader.LoadCustomBeatmapLevelAsync), Methods)!, LoadLevel, true));
            _hooks.Add(new Hook(typeof(CustomLevelLoader).GetMethod(nameof(CustomLevelLoader.LoadDifficultyBeatmapAsync), Methods)!, LoadDifficulty, true));
            _hooks.Add(new Hook(typeof(CustomLevelLoader).GetMethod(nameof(CustomLevelLoader.LoadBeatmapDataBasicInfo), Methods)!, LoadSaveData, true));
            _hooks.Add(new Hook(typeof(BeatmapDataLoader).GetMethod(nameof(BeatmapDataLoader.GetBeatmapDataFromSaveData), Methods)!, ConvertSaveData, true));
            _hooks.Add(new Hook(typeof(BeatmapDataLoader.BpmTimeProcessor).GetMethod(nameof(BeatmapDataLoader.BpmTimeProcessor.ConvertBeatToTime))!, BeatToSeconds, true));
        }

        public void Dispose()
        {
            for (int index = _hooks.Count - 1; index >= 0; index--) _hooks[index].Dispose();
            _hooks.Clear();
        }

        internal static CustomPreviewBeatmapLevel CreatePreview(CustomLevelLoader loader, V4LevelInfo info, string directory, string id, bool previewAudio = true)
        {
            var projected = StandardLevelInfoSaveData.DeserializeFromJSONString(info.LegacyMetadata(previewAudio).ToString(Formatting.None));
            var context = new LevelContext(info, directory, projected);
            Metadata.Add(projected, context);
            var sets = projected.difficultyBeatmapSets.Select(set => new PreviewDifficultyBeatmapSet(
                loader._beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(set.beatmapCharacteristicName),
                set.difficultyBeatmaps.Select(difficulty => (BeatmapDifficulty)Enum.Parse(typeof(BeatmapDifficulty), difficulty.difficulty)).ToArray())).ToArray();
            var preview = new CustomPreviewBeatmapLevel(Loader.defaultCoverImage, projected, directory, loader._cachedMediaAsyncLoader,
                id, projected.songName, projected.songSubName, projected.songAuthorName, projected.levelAuthorName,
                projected.beatsPerMinute, projected.songTimeOffset, projected.shuffle, projected.shufflePeriod,
                projected.previewStartTime, projected.previewDuration, loader.LoadEnvironmentInfo(projected.environmentName, false),
                loader.LoadEnvironmentInfo(projected.allDirectionsEnvironmentName, true), sets);
            Levels.Add(preview, context);
            return preview;
        }

        private EnvironmentInfoSO EnvironmentForDifficulty(Func<IDifficultyBeatmap, EnvironmentInfoSO> original, IDifficultyBeatmap key)
        {
            var song = key?.level == null ? null : Collections.GetCustomLevelSongData(key.level.levelID);
            var difficulty = key == null ? null : Collections.GetCustomLevelSongDifficultyData(key);
            if (difficulty?._environmentNameIdx is int index && index >= 0 && song != null && index < song._environmentNames.Length)
            {
                string name = song._environmentNames[index];
                var environment = _environments.environmentInfos.FirstOrDefault(value => value.serializedName == name);
                if (environment != null) return environment;
            }
            return original(key!);
        }

        private Task<CustomBeatmapLevel> LoadLevel(Func<CustomLevelLoader, CustomPreviewBeatmapLevel, CancellationToken, Task<CustomBeatmapLevel>> original,
            CustomLevelLoader loader, CustomPreviewBeatmapLevel preview, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Levels.TryGetValue(preview, out var context))
            {
                var fullPreview = CreatePreview(loader, context.Info, context.Directory, preview.levelID, false);
                fullPreview.SetField("<songDuration>k__BackingField", preview.songDuration);
                preview = fullPreview;
            }
            return original(loader, preview, cancellationToken);
        }

        private async Task<CustomDifficultyBeatmap> LoadDifficulty(
            Func<CustomLevelLoader, string, CustomBeatmapLevel, CustomDifficultyBeatmapSet, StandardLevelInfoSaveData, StandardLevelInfoSaveData.DifficultyBeatmap, CancellationToken, Task<CustomDifficultyBeatmap>> original,
            CustomLevelLoader loader, string path, CustomBeatmapLevel level, CustomDifficultyBeatmapSet set, StandardLevelInfoSaveData info,
            StandardLevelInfoSaveData.DifficultyBeatmap difficulty, CancellationToken cancellationToken)
        {
            DifficultyContext? previous = Loading.Value;
            try
            {
                Loading.Value = Metadata.TryGetValue(info, out var context) && context.Difficulties.TryGetValue(difficulty, out var descriptor)
                    ? new DifficultyContext(context, descriptor, difficulty) : null;
                return await original(loader, path, level, set, info, difficulty, cancellationToken);
            }
            finally { Loading.Value = previous; }
        }

        private Tuple<BeatmapSaveData, BeatmapDataBasicInfo> LoadSaveData(
            Func<CustomLevelLoader, string, string, StandardLevelInfoSaveData, Tuple<BeatmapSaveData, BeatmapDataBasicInfo>> original,
            CustomLevelLoader loader, string directory, string filename, StandardLevelInfoSaveData info)
        {
            DifficultyContext? context = Loading.Value;
            if (context == null) return original(loader, directory, filename, info);
            V4LevelInfo v4 = context.Level.Info;
            var projection = new V4LegacyProjection(
                V4Documents.Read(v4.ResolveAsset(context.Level.Directory, context.Difficulty.BeatmapFilename)),
                V4Documents.Read(v4.ResolveAsset(context.Level.Directory, context.Difficulty.LightshowFilename)),
                V4Documents.Read(v4.ResolveAsset(context.Level.Directory, v4.AudioDataFilename)), v4.Bpm);
            var save = Version3CustomBeatmapSaveData.DeserializeJson(projection.SaveData.ToString(Formatting.None),
                (context.NativeDifficulty as CustomLevelInfoSaveData.DifficultyBeatmap)?.customData ?? new CustomData(),
                (info as CustomLevelInfoSaveData)?.customData ?? new CustomData());
            Projections.Add(save, projection);
            V4LightingData.Register(save, projection);
            var relativeSpeed = new V4RelativeNjs(projection);
            if (relativeSpeed.HasEvents) RelativeSpeeds.Add(save, relativeSpeed);
            V4LegacyProjection? previous = Converting.Value;
            try
            {
                Converting.Value = projection;
                return Tuple.Create<BeatmapSaveData, BeatmapDataBasicInfo>(save, BeatmapDataLoader.GetBeatmapDataBasicInfoFromSaveData(save));
            }
            finally { Converting.Value = previous; }
        }

        private BeatmapData ConvertSaveData(
            Func<BeatmapSaveData, BeatmapDifficulty, float, bool, EnvironmentInfoSO, PlayerSpecificSettings, BeatmapData> original,
            BeatmapSaveData save, BeatmapDifficulty difficulty, float bpm, bool designatedEnvironment, EnvironmentInfoSO environment, PlayerSpecificSettings settings)
        {
            V4LegacyProjection? previous = Converting.Value;
            try
            {
                Converting.Value = Projections.TryGetValue(save, out var projection) ? projection : null;
                BeatmapData result = original(save, difficulty, bpm, designatedEnvironment, environment, settings);
                if (projection != null) V4LightingData.ResolvePrevious(result);
                return result;
            }
            finally { Converting.Value = previous; }
        }

        private float BeatToSeconds(Func<BeatmapDataLoader.BpmTimeProcessor, float, float> original, BeatmapDataLoader.BpmTimeProcessor processor, float beat) =>
            Converting.Value is V4LegacyProjection projection ? (float)projection.Tempo.BeatToSeconds(beat) : original(processor, beat);

        internal static V4RelativeNjs? RelativeSpeedFor(IDifficultyBeatmap difficulty) =>
            difficulty is global::CustomDifficultyBeatmap native && RelativeSpeeds.TryGetValue(native.beatmapSaveData, out var speed) ? speed : null;

        internal static bool IsV4(IDifficultyBeatmap difficulty) =>
            difficulty is global::CustomDifficultyBeatmap native && Projections.TryGetValue(native.beatmapSaveData, out _);

        private sealed class LevelContext
        {
            internal readonly V4LevelInfo Info;
            internal readonly string Directory;
            internal readonly Dictionary<StandardLevelInfoSaveData.DifficultyBeatmap, V4LevelInfo.Difficulty> Difficulties = new();
            internal LevelContext(V4LevelInfo info, string directory, StandardLevelInfoSaveData projected)
            {
                Info = info; Directory = directory;
                foreach (var set in projected.difficultyBeatmapSets)
                    foreach (var difficulty in set.difficultyBeatmaps)
                        Difficulties.Add(difficulty, info.Difficulties.Single(value => value.Characteristic == set.beatmapCharacteristicName && value.Name == difficulty.difficulty));
            }
        }

        private sealed record DifficultyContext(LevelContext Level, V4LevelInfo.Difficulty Difficulty, StandardLevelInfoSaveData.DifficultyBeatmap NativeDifficulty);
    }
}
#endif
