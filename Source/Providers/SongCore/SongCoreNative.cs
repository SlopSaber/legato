#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace SongCore
{
    internal static class SongCoreNative
    {
        internal static string KeyLevelId(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key?.level?.levelID ?? string.Empty;
#else
            key.levelId;
#endif
        internal static bool KeyIsValid(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key?.level != null && key.parentDifficultyBeatmapSet?.beatmapCharacteristic != null;
#else
            key.IsValid();
#endif
        internal static bool IsHiddenDirectory(DirectoryInfo directory) =>
#if BEAT_SABER_1_29_0
            (directory.Attributes & FileAttributes.Hidden) != 0 || directory.Name.StartsWith(".", StringComparison.Ordinal) || directory.Name.StartsWith("~", StringComparison.Ordinal);
#else
            CustomLevelPathHelper.IsHiddenDirectory(directory);
#endif
        internal static IEnumerable<string> Authors(BeatmapLevel level)
        {
#if BEAT_SABER_1_29_0
            if (Loader.NativeSaveStore.TryGetValue(level.levelID, out var data) && data.beatmapLevelSaveData != null)
                return data.beatmapLevelSaveData.Difficulties.SelectMany(value => value.Mappers.Concat(value.Lighters)).Distinct();
            return new[] { level.levelAuthorName };
#else
            return level.allMappers.Concat(level.allLighters).Distinct();
#endif
        }
        internal static string PackId(AnnotatedBeatmapLevelCollectionCell cell) =>
#if BEAT_SABER_1_29_0
            (cell._annotatedBeatmapLevelCollection as IBeatmapLevelPack)?.packID ?? string.Empty;
#else
            cell._beatmapLevelPack.packID;
#endif
        internal static EnvironmentInfoSO? Environment(EnvironmentsListModel model, string name) =>
#if BEAT_SABER_1_29_0
            model.GetEnvironmentInfoBySerializedName(name);
#else
            model.GetEnvironmentInfoBySerializedNameSafe(name);
#endif
        internal static List<BeatmapCharacteristic> AvailableCharacteristics(BeatmapCharacteristicSegmentedControlController controller) =>
#if BEAT_SABER_1_29_0
            controller._beatmapCharacteristics;
#else
            controller._currentlyAvailableBeatmapCharacteristics;
#endif
        internal static BeatmapKey SelectedKey(StandardLevelDetailView view) =>
#if BEAT_SABER_1_29_0
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static string LevelDirectory(string id) => Loader.NativeSaveStore[id].customLevelFolderInfo.folderPath;
    }
}

#if BEAT_SABER_1_29_0
namespace SongCore
{
    internal static class LegacyCharacteristicNames
    {
        internal static string SerializedName(this BeatmapCharacteristicSO value) => value.serializedName;
    }
}
#endif
