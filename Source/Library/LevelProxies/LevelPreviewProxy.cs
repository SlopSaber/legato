#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps {
    /// <summary>Preserves a level preview through the selected native API.</summary>
    public abstract class LevelPreviewProxy : BeatmapLevel {
#if BEAT_SABER_1_29_0
        internal BeatmapLevel Source { get; }
        /// <summary>Wraps the supplied native level preview.</summary>
        protected LevelPreviewProxy(BeatmapLevel source) { Source = source; }
        /// <inheritdoc/>
        public string levelID => Source.levelID;
        /// <inheritdoc/>
        public string songName => Source.songName;
        /// <inheritdoc/>
        public string songSubName => Source.songSubName;
        /// <inheritdoc/>
        public string songAuthorName => Source.songAuthorName;
        /// <inheritdoc/>
        public string levelAuthorName => Source.levelAuthorName;
        /// <inheritdoc/>
        public float beatsPerMinute => Source.beatsPerMinute;
        /// <inheritdoc/>
        public float songTimeOffset => Source.songTimeOffset;
        /// <inheritdoc/>
        public float shuffle => Source.shuffle;
        /// <inheritdoc/>
        public float shufflePeriod => Source.shufflePeriod;
        /// <inheritdoc/>
        public float previewStartTime => Source.previewStartTime;
        /// <inheritdoc/>
        public float previewDuration => Source.previewDuration;
        /// <inheritdoc/>
        public float songDuration => Source.songDuration;
        /// <inheritdoc/>
        public EnvironmentInfoSO environmentInfo => Source.environmentInfo;
        /// <inheritdoc/>
        public EnvironmentInfoSO allDirectionsEnvironmentInfo => Source.allDirectionsEnvironmentInfo;
        /// <inheritdoc/>
        public IReadOnlyList<PreviewDifficultyBeatmapSet> previewDifficultyBeatmapSets => Source.previewDifficultyBeatmapSets;
        /// <inheritdoc/>
        public Task<Sprite> GetCoverImageAsync(CancellationToken token) => Source.GetCoverImageAsync(token);
#else
        /// <summary>Wraps the supplied native level preview.</summary>
        protected LevelPreviewProxy(BeatmapLevel source)
            : base(source.version, source.hasPrecalculatedData, source.levelID, source.songName, source.songSubName,
                source.songAuthorName, source.allMappers, source.allLighters, source.beatsPerMinute, source.integratedLufs,
                source.songTimeOffset, source.previewStartTime, source.previewDuration, source.songDuration,
                source.contentRating, source.previewMediaData, BeatmapLevelDataCopy.CopyBasicData(source)) { }
#endif
    }

    /// <summary>Wraps preview levels as a native level pack.</summary>
    public abstract class LevelPackProxy :
#if BEAT_SABER_1_29_0
        IBeatmapLevelPack {
        /// <inheritdoc/>
        public string packID { get; }
        /// <inheritdoc/>
        public string packName { get; }
        /// <inheritdoc/>
        public string shortPackName => packName;
        /// <inheritdoc/>
        public string collectionName => packName;
        /// <inheritdoc/>
        public Sprite? coverImage { get; }
        /// <inheritdoc/>
        public Sprite? smallCoverImage { get; }
        /// <inheritdoc/>
        public IBeatmapLevelCollection beatmapLevelCollection { get; }
        /// <inheritdoc/>
        public bool isPackAlwaysOwned => true;
        /// <summary>Creates a pack with the supplied identity, cover images and levels.</summary>
        protected LevelPackProxy(string id, string title, Sprite? cover, Sprite? smallCover, BeatmapLevel[] levels) {
            packID = id;
            packName = title;
            coverImage = cover;
            smallCoverImage = smallCover;
            beatmapLevelCollection = new BeatmapLevelCollection(levels);
        }
#else
        BeatmapLevelPack {
        /// <summary>Creates a pack with the supplied identity, cover images and levels.</summary>
        protected LevelPackProxy(string id, string title, Sprite? cover, Sprite? smallCover, BeatmapLevel[] levels)
            : base(id, title, title, cover, smallCover,
#if !BEAT_SABER_1_37_1
                PackBuyOption.Default,
#endif
                levels, PlayerSensitivityFlag.Unknown) { }
#endif
    }
}
