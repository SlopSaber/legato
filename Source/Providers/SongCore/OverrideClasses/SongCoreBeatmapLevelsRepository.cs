#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections.Generic;
using SongCore.Utilities;

namespace SongCore.OverrideClasses
{
    public class SongCoreBeatmapLevelsRepository : BeatmapLevelsRepository
    {
        private readonly List<NativeSongPack> _customBeatmapLevelPacks = new();

        private SongCoreBeatmapLevelsRepository(IEnumerable<NativeSongPack> beatmapLevelPacks)
            : base(beatmapLevelPacks)
        {
        }

        public static SongCoreBeatmapLevelsRepository CreateNew()
        {
            return new SongCoreBeatmapLevelsRepository(Array.Empty<NativeSongPack>());
        }

        public void AddLevelPack(NativeSongPack pack)
        {
            if (pack != null && !_customBeatmapLevelPacks.Contains(pack))
            {
                _customBeatmapLevelPacks.Add(pack);
                RefreshCollections();
            }
        }

        public void RemoveLevelPack(NativeSongPack pack)
        {
            if (pack != null && _customBeatmapLevelPacks.Contains(pack))
            {
                _customBeatmapLevelPacks.Remove(pack);
                RefreshCollections();
            }
        }

        public void ClearLevelPacks()
        {
            _customBeatmapLevelPacks.Clear();
        }

        private void RefreshCollections()
        {
            _idToBeatmapLevelPack.Clear();
            _idToBeatmapLevel.Clear();
            _beatmapLevelIdToBeatmapLevelPackId.Clear();

            var that = (BeatmapLevelsRepository)this;
            Accessors.BeatmapLevelPacksAccessor(ref that) = _customBeatmapLevelPacks.ToArray();
            foreach (var beatmapLevelPack in _beatmapLevelPacks)
            {
                _idToBeatmapLevelPack.Add(beatmapLevelPack.packID, beatmapLevelPack);
                foreach (var beatmapLevel in SongCoreNative.PackLevels(beatmapLevelPack))
                {
                    _beatmapLevelIdToBeatmapLevelPackId.TryAdd(beatmapLevel.levelID, beatmapLevelPack.packID);
                    _idToBeatmapLevel.TryAdd(beatmapLevel.levelID, beatmapLevel);
                }
            }
        }

    }
}
#endif
