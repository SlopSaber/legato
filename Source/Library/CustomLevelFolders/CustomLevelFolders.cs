#nullable enable
using System.Collections;
using System.Reflection;

namespace Legato.Beatmaps {
    internal static class CustomLevelFolders {
#if !BEAT_SABER_1_29_0
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo? SaveData = typeof(CustomLevelLoader).GetField("_loadedBeatmapSaveData", Fields);
#endif
        internal static bool TryGetPath(CustomLevelLoader loader, BeatmapLevel level, out string? path) {
#if BEAT_SABER_1_29_0
            path = level is CustomPreviewBeatmapLevel preview ? preview.customLevelPath
                : level is CustomBeatmapLevel full ? full.customLevelPath : null;
#else
            var loaded = (SaveData?.GetValue(loader) as IDictionary)?[level.levelID];
            var folder = loaded?.GetType().GetField("customLevelFolderInfo", Fields)?.GetValue(loaded);
            path = folder?.GetType().GetField("folderPath", Fields)?.GetValue(folder) as string;
#endif
            return !string.IsNullOrEmpty(path);
        }
    }
}
