#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
#nullable enable
namespace Legato.Beatmaps.Formats
{
    public sealed class LegacyLevelFolderInfo
    {
        public readonly string folderPath;
        public readonly string folderName;
        public readonly string levelInfoJsonString;

        public LegacyLevelFolderInfo(string path, string name, string json)
        {
            folderPath = path; folderName = name; levelInfoJsonString = json;
        }
    }

    public sealed class LegacyLoadedSaveData
    {
        public LegacyLevelFolderInfo customLevelFolderInfo = null!;
        public StandardLevelInfoSaveData? standardLevelInfoSaveData;
        public V4LevelInfo? beatmapLevelSaveData;
    }
}
#endif
