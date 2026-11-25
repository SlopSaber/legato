#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System.IO;
using System.Linq;
using SongCore.Data;
using SongCore.Hooks.BeatmapLevelCache;

namespace SongCore.Utilities
{
    public partial class Hashing
    {
        public static string ComputeCustomLevelHash(CustomLevelFolderInfo folder, NativeV4LevelInfo info)
        {
            if (GetCachedSongData(folder.folderPath, out long directoryHash, out string cached)) return cached;
            string infoPath = Path.Combine(folder.folderPath, CustomLevelPathHelper.kStandardLevelInfoFilename);
            string audio = info.ResolveAsset(folder.folderPath, info.AudioDataFilename);
            var files = info.Difficulties.SelectMany(difficulty => new[]
            {
                info.ResolveAsset(folder.folderPath, difficulty.BeatmapFilename),
                info.ResolveAsset(folder.folderPath, difficulty.LightshowFilename)
            }).Prepend(audio).Where(File.Exists).Prepend(infoPath);
            IOBlacklistHook.AllowIO.Value = true;
            try
            {
                string hash = CreateSha1FromLevelFiles(files);
                TryGetRelativePath(folder.folderPath, out string relative);
                cachedSongHashData[relative] = new SongHashData(directoryHash, hash);
                return hash;
            }
            finally { IOBlacklistHook.AllowIO.Value = false; }
        }
    }
}
#endif
