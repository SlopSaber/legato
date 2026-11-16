#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Legato.Beatmaps {
    internal static class LevelMetadata {
        internal static string Authors(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level.levelAuthorName;
#else
            string.Join(", ", level.allMappers.Concat(level.allLighters).Distinct());
#endif
        internal static BeatmapDifficulty Difficulty(BeatmapKey? key) => key?.difficulty ?? default;
        internal static string Characteristic(BeatmapKey? key) =>
#if BEAT_SABER_1_29_0
            key == null ? string.Empty : BeatmapKeyExtensions.CharacteristicSerializedName(key);
#else
            key.HasValue ? BeatmapKeyExtensions.CharacteristicSerializedName(key.Value) : string.Empty;
#endif
        internal static Stream? OpenCustomCover(BeatmapLevel? level, object? loader) {
            string? directory = null;
            string? filename = null;
#if BEAT_SABER_1_29_0
            while (level is LevelPreviewProxy proxy) level = proxy.Source;
            if (level is CustomPreviewBeatmapLevel preview) {
                directory = preview.customLevelPath;
                filename = preview.standardLevelInfoSaveData.coverImageFilename;
            } else if (level is CustomBeatmapLevel full) {
                directory = full.customLevelPath;
                filename = full.standardLevelInfoSaveData.coverImageFilename;
            }
#else
            if (level != null && !level.hasPrecalculatedData && loader != null) {
                var field = typeof(CustomLevelLoader).GetField("_loadedBeatmapSaveData", BindingFlags.Instance | BindingFlags.NonPublic);
                var loaded = (field?.GetValue(loader) as IDictionary)?[level.levelID];
                var info = loaded?.GetType().GetField("standardLevelInfoSaveData")?.GetValue(loaded);
                var folder = loaded?.GetType().GetField("customLevelFolderInfo")?.GetValue(loaded);
                filename = info?.GetType().GetProperty("coverImageFilename")?.GetValue(info) as string;
                directory = folder?.GetType().GetField("folderPath")?.GetValue(folder) as string;
            }
#endif
            return string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(directory) ? null
                : new FileStream(Path.Combine(directory!, filename!), FileMode.Open, FileAccess.Read, FileShare.Read, 0x4096, true);
        }
    }
}
