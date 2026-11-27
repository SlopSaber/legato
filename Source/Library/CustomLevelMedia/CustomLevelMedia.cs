#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IPA.Utilities;

namespace Legato.Beatmaps {
    internal static class CustomLevelMedia {
#if LEGATO_EARLY_STANDARD_MAPS
        internal readonly struct EarlyMedia {
            internal EarlyMedia(string audioPath, string? coverPath, string songName, string songAuthorName) {
                AudioPath = audioPath;
                CoverPath = coverPath;
                SongName = songName;
                SongAuthorName = songAuthorName;
            }

            internal string AudioPath { get; }
            internal string? CoverPath { get; }
            internal string SongName { get; }
            internal string SongAuthorName { get; }
        }

        internal static List<EarlyMedia> ReadFolder(string root) {
            var result = new List<EarlyMedia>();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return result;

            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (string directory in Directory.GetDirectories(root)) {
                try {
                    EarlyMapEntry? entry = EarlyMapFiles.ReadLocal(root, directory);
                    if (entry == null) continue;

                    string? audio = entry.Info.difficultyLevels?.Select(chart => chart?.audioPath).FirstOrDefault(path => !string.IsNullOrEmpty(path));
                    string? audioPath = EarlyMapFiles.LocalAsset(entry.DirectoryPath, audio);
                    if (audioPath == null) continue;

                    result.Add(new EarlyMedia(audioPath, EarlyMapFiles.LocalAsset(entry.DirectoryPath, entry.Info.coverImagePath),
                        string.IsNullOrEmpty(entry.Info.songName) ? Path.GetFileName(entry.DirectoryPath) : entry.Info.songName,
                        entry.Info.authorName ?? string.Empty));
                } catch (Exception) {
                }
            }
            return result;
        }
#else
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
        private static readonly FieldAccessor<FileSystemPreviewMediaData, string>.Accessor Audio =
            FieldAccessor<FileSystemPreviewMediaData, string>.GetAccessor("_previewAudioClipPath");
        private static readonly FieldAccessor<FileSystemPreviewMediaData, string>.Accessor Cover =
            FieldAccessor<FileSystemPreviewMediaData, string>.GetAccessor("_coverSpritePath");
#endif
        internal static bool TryGetPaths(BeatmapLevel level, out string? audioPath, out string? coverPath) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
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
#endif
    }
}
