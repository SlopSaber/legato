#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
using System.IO;
using System.Linq;
using SongCore.Data;
using SongCore.Hooks.BeatmapLevelCache;
namespace SongCore.Utilities
{
    public partial class Hashing
    {
        public static string ComputeCustomLevelHash(CustomLevelFolderInfo customLevelFolderInfo, NativeV4LevelInfo beatmapLevelSaveData)
        {
            if (GetCachedSongData(customLevelFolderInfo.folderPath, out var directoryHash, out var songHash))
            {
                return songHash;
            }

            var infoPath = Path.Combine(customLevelFolderInfo.folderPath, CustomLevelPathHelper.kStandardLevelInfoFilename);
            var audioDataPath = Path.Combine(customLevelFolderInfo.folderPath, beatmapLevelSaveData.audio.audioDataFilename);
            var files = beatmapLevelSaveData.difficultyBeatmaps.SelectMany(difficultyBeatmap => new[]
            {
                Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.beatmapDataFilename),
                Path.Combine(customLevelFolderInfo.folderPath, difficultyBeatmap.lightshowDataFilename)
            }).Prepend(audioDataPath).Where(File.Exists).Prepend(infoPath);

            IOBlacklistHook.AllowIO.Value = true;
            try
            {
                var hash = CreateSha1FromLevelFiles(files);
                TryGetRelativePath(customLevelFolderInfo.folderPath, out var relativePath);
                cachedSongHashData[relativePath] = new SongHashData(directoryHash, hash);

                return hash;
            }
            finally
            {
                IOBlacklistHook.AllowIO.Value = false;
            }
        }
    }
}
#endif
