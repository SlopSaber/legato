#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps {
    public abstract class LevelPreviewProxy : BeatmapLevel {
#if BEAT_SABER_1_29_0
        internal BeatmapLevel Source { get; }
        protected LevelPreviewProxy(BeatmapLevel source) { Source = source; }
        public string levelID => Source.levelID;
        public string songName => Source.songName;
        public string songSubName => Source.songSubName;
        public string songAuthorName => Source.songAuthorName;
        public string levelAuthorName => Source.levelAuthorName;
        public float beatsPerMinute => Source.beatsPerMinute;
        public float songTimeOffset => Source.songTimeOffset;
        public float shuffle => Source.shuffle;
        public float shufflePeriod => Source.shufflePeriod;
        public float previewStartTime => Source.previewStartTime;
        public float previewDuration => Source.previewDuration;
        public float songDuration => Source.songDuration;
        public EnvironmentInfoSO environmentInfo => Source.environmentInfo;
        public EnvironmentInfoSO allDirectionsEnvironmentInfo => Source.allDirectionsEnvironmentInfo;
        public IReadOnlyList<PreviewDifficultyBeatmapSet> previewDifficultyBeatmapSets => Source.previewDifficultyBeatmapSets;
        public Task<Sprite> GetCoverImageAsync(CancellationToken token) => Source.GetCoverImageAsync(token);
#else
        protected LevelPreviewProxy(BeatmapLevel source)
            : base(source.version, source.hasPrecalculatedData, source.levelID, source.songName, source.songSubName,
                source.songAuthorName, source.allMappers, source.allLighters, source.beatsPerMinute, source.integratedLufs,
                source.songTimeOffset, source.previewStartTime, source.previewDuration, source.songDuration,
                source.contentRating, source.previewMediaData, BeatmapLevelDataCopy.CopyBasicData(source)) { }
#endif
    }

    public abstract class LevelPackProxy :
#if BEAT_SABER_1_29_0
        CustomBeatmapLevelPack {
        protected LevelPackProxy(string id, string title, Sprite cover, Sprite smallCover, BeatmapLevel[] levels)
            : base(id, title, title, cover, smallCover, new CustomBeatmapLevelCollection(levels)) { }
#else
        BeatmapLevelPack {
        protected LevelPackProxy(string id, string title, Sprite cover, Sprite smallCover, BeatmapLevel[] levels)
            : base(id, title, title, cover, smallCover,
#if !BEAT_SABER_1_37_1
                PackBuyOption.Default,
#endif
                levels, PlayerSensitivityFlag.Unknown) { }
#endif
    }
}
