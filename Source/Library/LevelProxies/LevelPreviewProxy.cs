#nullable enable
#if !LEGATO_BEFORE_MULTIPLAYER && !LEGATO_EARLY_SCENE_SETUP
using System.Collections.Generic;
#endif
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps {
    /// <summary>Preserves a level preview through the selected native API.</summary>
    public abstract class LevelPreviewProxy : BeatmapLevel {
#if LEGATO_EARLY_SCENE_SETUP
        internal BeatmapLevel Source { get; }
        /// <summary>Wraps the supplied native level.</summary>
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
        public AudioClip audioClip => Source.audioClip;
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
        public Sprite coverImage => Source.coverImage;
        /// <inheritdoc/>
        public SceneInfo environmentSceneInfo => Source.environmentSceneInfo;
        /// <inheritdoc/>
        public IStandardLevelDifficultyBeatmap[] difficultyBeatmaps => Source.difficultyBeatmaps;
        /// <inheritdoc/>
        public IStandardLevelDifficultyBeatmap GetDifficultyLevel(LevelDifficulty difficulty) => Source.GetDifficultyLevel(difficulty);
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
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
#if LEGATO_BEFORE_MULTIPLAYER
        public PreviewDifficultyBeatmapSet[] previewDifficultyBeatmapSets => Source.previewDifficultyBeatmapSets;
        /// <inheritdoc/>
        public Task<AudioClip> GetPreviewAudioClipAsync(CancellationToken token) => Source.GetPreviewAudioClipAsync(token);
        /// <inheritdoc/>
        public Task<Texture2D> GetCoverImageTexture2DAsync(CancellationToken token) => Source.GetCoverImageTexture2DAsync(token);
#else
        public IReadOnlyList<PreviewDifficultyBeatmapSet> previewDifficultyBeatmapSets => Source.previewDifficultyBeatmapSets;
        /// <inheritdoc/>
        public Task<Sprite> GetCoverImageAsync(CancellationToken token) => Source.GetCoverImageAsync(token);
#endif
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
    public abstract class LevelPackProxy
#if LEGATO_EARLY_SCENE_SETUP
    {
        /// <summary>The stable identifier for the level group.</summary>
        public string packID { get; }
        /// <summary>The level group's display name.</summary>
        public string packName { get; }
        /// <summary>The full-sized cover image.</summary>
        public Sprite? coverImage { get; }
        /// <summary>The thumbnail cover image.</summary>
        public Sprite? smallCoverImage { get; }
        /// <summary>The loaded playlist entries.</summary>
        public BeatmapLevel[] beatmapLevels { get; }

        /// <summary>Creates a level group with the supplied identity, covers and playable levels.</summary>
        protected LevelPackProxy(string id, string title, Sprite? cover, Sprite? smallCover, BeatmapLevel[] levels) {
            packID = id;
            packName = title;
            coverImage = cover;
            smallCoverImage = smallCover;
            beatmapLevels = levels;
        }
    }
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        :
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
        :
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
