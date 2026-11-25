#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections.Generic;
#if !LEGATO_BEFORE_MULTIPLAYER
using IPA.Utilities;
#endif
using UnityEngine;

namespace SongCore.OverrideClasses
{
    public class SongCoreCustomBeatmapLevelPack : CustomBeatmapLevelPack
    {
#if LEGATO_BEFORE_MULTIPLAYER
        public SongCoreCustomBeatmapLevelPack(string packID, string packName, Sprite? coverImage, BeatmapLevel[] beatmapLevels, string shortPackName = "")
            : base(packID, packName, shortPackName.Length == 0 ? packName : shortPackName,
                CreateCover(coverImage), new CustomBeatmapLevelCollection(Array.ConvertAll(beatmapLevels, AsNativePreview)))
        {
        }

        public void UpdateBeatmapLevels(BeatmapLevel[] levels) =>
            _customBeatmapLevelCollection._customPreviewBeatmapLevels = Array.ConvertAll(levels, AsNativePreview);

        private static CustomPreviewBeatmapLevel AsNativePreview(BeatmapLevel level) => (CustomPreviewBeatmapLevel)level;
#else
        private readonly MutableCollection _levels;

        public SongCoreCustomBeatmapLevelPack(string packID, string packName, Sprite? coverImage, BeatmapLevel[] beatmapLevels, string shortPackName = "")
            : base(packID, packName, shortPackName.Length == 0 ? packName : shortPackName,
                CreateCover(coverImage), coverImage ?? Loader.defaultCoverImage, new CustomBeatmapLevelCollection(Array.Empty<CustomPreviewBeatmapLevel>()))
        {
            _levels = new MutableCollection(beatmapLevels);
            ((CustomBeatmapLevelPack)this).SetField("<beatmapLevelCollection>k__BackingField", (IBeatmapLevelCollection)_levels);
        }
#endif

        private static Sprite CreateCover(Sprite? cover)
        {
            cover ??= Loader.defaultCoverImage;
            if (cover == null) throw new InvalidOperationException("SongCore cover sprites have not been initialized.");
            return cover == Loader.defaultCoverImage ? cover : Sprite.Create(cover.texture, cover.rect, cover.pivot, cover.texture.width);
        }

#if !LEGATO_BEFORE_MULTIPLAYER
        public void UpdateBeatmapLevels(BeatmapLevel[] levels) => _levels.Levels = levels;

        private sealed class MutableCollection : IBeatmapLevelCollection
        {
            internal BeatmapLevel[] Levels;
            internal MutableCollection(BeatmapLevel[] levels) => Levels = levels;
            public IReadOnlyList<BeatmapLevel> beatmapLevels => Levels;
        }
#endif
    }

    public class SongCoreBeatmapLevelsRepository : IBeatmapLevelPackCollection
    {
        private readonly List<IBeatmapLevelPack> _packs = new();
        public IBeatmapLevelPack[] beatmapLevelPacks { get; private set; } = Array.Empty<IBeatmapLevelPack>();

        public static SongCoreBeatmapLevelsRepository CreateNew() => new();

        public void AddLevelPack(IBeatmapLevelPack pack)
        {
            if (pack == null || _packs.Contains(pack)) return;
            _packs.Add(pack);
            beatmapLevelPacks = _packs.ToArray();
        }

        public void RemoveLevelPack(IBeatmapLevelPack pack)
        {
            if (_packs.Remove(pack)) beatmapLevelPacks = _packs.ToArray();
        }

        public void ClearLevelPacks()
        {
            _packs.Clear();
            beatmapLevelPacks = Array.Empty<IBeatmapLevelPack>();
        }
    }
}
#endif
