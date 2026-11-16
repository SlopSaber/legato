#nullable enable
using System.IO;
using IPA.Utilities;

namespace Legato.Beatmaps {
    internal static class CustomLevelMedia {
#if !BEAT_SABER_1_29_0
        private static readonly FieldAccessor<FileSystemPreviewMediaData, string>.Accessor Audio =
            FieldAccessor<FileSystemPreviewMediaData, string>.GetAccessor("_previewAudioClipPath");
        private static readonly FieldAccessor<FileSystemPreviewMediaData, string>.Accessor Cover =
            FieldAccessor<FileSystemPreviewMediaData, string>.GetAccessor("_coverSpritePath");
#endif
        internal static bool TryGetPaths(BeatmapLevel level, out string? audioPath, out string? coverPath) {
#if BEAT_SABER_1_29_0
            string? folder = null;
            string? audio = null;
            string? cover = null;
            if (level is CustomPreviewBeatmapLevel preview) {
                folder = preview.customLevelPath;
                audio = preview.standardLevelInfoSaveData.songFilename;
                cover = preview.standardLevelInfoSaveData.coverImageFilename;
            } else if (level is CustomBeatmapLevel full) {
                folder = full.customLevelPath;
                audio = full.standardLevelInfoSaveData.songFilename;
                cover = full.standardLevelInfoSaveData.coverImageFilename;
            }
            audioPath = string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(audio) ? null : Path.Combine(folder!, audio!);
            coverPath = string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(cover) ? null : Path.Combine(folder!, cover!);
#else
            if (level.previewMediaData is FileSystemPreviewMediaData media) {
                audioPath = Audio(ref media);
                coverPath = Cover(ref media);
            } else {
                audioPath = null;
                coverPath = null;
            }
#endif
            return !string.IsNullOrEmpty(audioPath);
        }
    }
}
