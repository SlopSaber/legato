#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
#nullable enable
using System;
using System.IO;
using System.Linq;
using Legato.Beatmaps.Formats;
using Newtonsoft.Json.Linq;
using SongCore.Data;
using SongCore.Hooks.BeatmapLevelCache;
using SongCore.Utilities;
using IPA.Utilities;
using UnityEngine;
namespace SongCore
{
    public partial class Loader
    {
        private void GetSongDuration(NativeLoadedSaveData data, BeatmapLevel level)
        {
            try
            {
                Hashing.TryGetRelativePath(data.customLevelFolderInfo.folderPath, out var path);
                float duration = Hashing.cachedAudioData.TryGetValue(path, out var cache) && cache.id == level.levelID ? cache.duration : 0;
                if (duration <= 0)
                {
                    bool previous = IOBlacklistHook.AllowIO.Value;
                    IOBlacklistHook.AllowIO.Value = true;
                    try
                    {
                        if (data.beatmapLevelSaveData is V4LevelInfo v4)
                            duration = (float)new V4TempoMap(V4Documents.Read(v4.ResolveAsset(data.customLevelFolderInfo.folderPath, v4.AudioDataFilename)), v4.Bpm).Duration;
                        else
                            duration = GetLengthFromAudio(Path.Combine(data.customLevelFolderInfo.folderPath, data.standardLevelInfoSaveData!.songFilename));
                        if (duration <= 1) duration = GetLengthFromMap(data, null!, level);
                    }
                    finally { IOBlacklistHook.AllowIO.Value = previous; }
                    Hashing.cachedAudioData[path] = new AudioCacheData(level.levelID, duration);
                }
                var preview = (CustomPreviewBeatmapLevel)level;
#if LEGATO_BEFORE_MULTIPLAYER
                preview._songDuration = duration;
                if (_config.ForceLongPreviews)
                    preview._previewDuration = Mathf.Max(preview.previewDuration, duration - preview.previewStartTime);
#else
                preview.SetField("<songDuration>k__BackingField", duration);
                if (_config.ForceLongPreviews && data.beatmapLevelSaveData?.PreviewFilename == data.beatmapLevelSaveData?.AudioFilename)
                    preview.SetField("<previewDuration>k__BackingField", Mathf.Max(preview.previewDuration, duration - preview.previewStartTime));
#endif
            }
            catch (Exception error)
            {
                Plugin.Log.Warn("Failed to parse song duration");
                Plugin.Log.Warn(error);
            }
        }

        public static float GetLengthFromMap(NativeLoadedSaveData data, IBeatmapLevelData levelData, BeatmapLevel level)
        {
            string directory = data.customLevelFolderInfo.folderPath;
            if (data.beatmapLevelSaveData is V4LevelInfo v4)
            {
                var tempo = new V4TempoMap(V4Documents.Read(v4.ResolveAsset(directory, v4.AudioDataFilename)), v4.Bpm);
                if (tempo.Duration > 0) return (float)tempo.Duration;
                var difficulty = v4.Difficulties.Last();
                var gameplay = V4Documents.Read(v4.ResolveAsset(directory, difficulty.BeatmapFilename));
                var lightshow = V4Documents.Read(v4.ResolveAsset(directory, difficulty.LightshowFilename));
                var notes = V4Documents.Objects(gameplay, "colorNotes").ToArray();
                double beat = notes.Length > 0 ? notes.Max(value => V4Documents.Number(value["b"], 0, "b"))
                    : V4Documents.Objects(lightshow, "basicEvents").Select(value => V4Documents.Number(value["b"], 0, "b")).DefaultIfEmpty().Max();
                return (float)tempo.BeatToSeconds(beat);
            }
            var info = data.standardLevelInfoSaveData!;
            string filename = info.difficultyBeatmapSets.Last().difficultyBeatmaps.Last().beatmapFilename;
#if LEGATO_BEFORE_MULTIPLAYER
            string json = File.ReadAllText(Path.Combine(directory, filename));
            BeatmapData chart = new BeatmapDataLoader().GetBeatmapDataFromJson(json, level.beatsPerMinute, level.shuffle, level.shufflePeriod);
            float lastEvent = chart.beatmapEventDatas.Select(value => value.time).DefaultIfEmpty().Max();
            float lastObject = chart.beatmapLinesData.SelectMany(line => line.beatmapObjectsData).Select(value => value.time).DefaultIfEmpty().Max();
            return Mathf.Max(lastObject, lastEvent);
#else
            var save = CustomJSONData.CustomBeatmap.Version3CustomBeatmapSaveData.Deserialize(Path.Combine(directory, filename), new CustomJSONData.CustomBeatmap.CustomData(), new CustomJSONData.CustomBeatmap.CustomData());
            float highest = save.colorNotes.Count > 0 ? save.colorNotes.Max(value => value.beat)
                : save.basicBeatmapEvents.Select(value => value.beat).DefaultIfEmpty().Max();
            return new BeatmapDataLoader.BpmTimeProcessor(level.beatsPerMinute, save.bpmEvents).ConvertBeatToTime(highest);
#endif
        }
    }
}
#endif
