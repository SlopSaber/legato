#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps
{
    internal sealed class EarlyLevelCatalog
    {
        private const string InfoFileName = "info.json";
        private readonly Dictionary<string, StandardLevelSO> _loaded;
        private readonly Dictionary<string, CustomLevelInfoWrapper> _byPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CustomLevelInfoWrapper> _byHash = new(StringComparer.OrdinalIgnoreCase);

        private EarlyLevelCatalog(Dictionary<string, StandardLevelSO> loaded)
        {
            _loaded = loaded;
        }

        internal static BeatmapLevel? FindLoaded(string? levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            return Resources.FindObjectsOfTypeAll<StandardLevelSO>()
                .FirstOrDefault(level => level && string.Equals(level.levelID, levelId, StringComparison.OrdinalIgnoreCase));
        }

        internal static Task<EarlyLevelCatalog> ReadAsync(bool indexHashes, CancellationToken cancellationToken)
        {
            var loaded = new Dictionary<string, StandardLevelSO>(StringComparer.OrdinalIgnoreCase);
            foreach (StandardLevelSO level in Resources.FindObjectsOfTypeAll<StandardLevelSO>())
            {
                if (level && !string.IsNullOrEmpty(level.levelID)) loaded[level.levelID] = level;
            }

            string root = CustomLevelsModelSO.customLevelsDirectoryPath;
            var catalog = new EarlyLevelCatalog(loaded);
            return Task.Run(() =>
            {
                catalog.ReadCustomLevels(root, indexHashes, cancellationToken);
                return catalog;
            }, cancellationToken);
        }

        internal async Task<BeatmapLevel?> LoadAsync(string? levelId, string? hash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (levelId != null && _loaded.TryGetValue(levelId, out var loaded) && loaded) return loaded;

            CustomLevelInfoWrapper? info = null;
            if (levelId != null) _byPath.TryGetValue(levelId, out info);
            if (info == null && hash != null) _byHash.TryGetValue(hash, out info);
            if (info == null) return null;

            if (!_loaded.TryGetValue(info.levelID, out loaded) || !loaded)
            {
                loaded = await EarlyCustomLevelLoader.LoadAsync(info, cancellationToken);
                if (loaded) _loaded[info.levelID] = loaded!;
            }
            return loaded;
        }

        internal static Stream? OpenCustomCover(BeatmapLevel? level)
        {
            string? directory = level?.levelID;
            if (string.IsNullOrEmpty(directory) || !Path.IsPathRooted(directory)) return null;
            string root = Path.GetFullPath(CustomLevelsModelSO.customLevelsDirectoryPath);
            directory = Path.GetFullPath(directory);
            if (!string.Equals(Path.GetDirectoryName(directory), root, StringComparison.OrdinalIgnoreCase) ||
                !Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0 ||
                LocalAsset(directory, InfoFileName) == null) return null;

            var entry = CustomLevelInfoWrapperLoader.LoadCustomLevelInfoWrapper(directory);
            string? path = entry == null ? null : LocalAsset(directory, entry.customLevelInfo.coverImagePath);
            return path == null ? null : File.OpenRead(path);
        }

        internal static string? LocalAsset(string directory, string? name)
        {
            if (string.IsNullOrEmpty(name) || Path.IsPathRooted(name)) return null;
            string path = Path.GetFullPath(Path.Combine(directory, name));
            if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(path)) return null;

            for (string? item = path; item != null && !string.Equals(item, directory, StringComparison.OrdinalIgnoreCase);
                 item = Path.GetDirectoryName(item))
            {
                if ((File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0) return null;
            }
            return path;
        }

        private void ReadCustomLevels(string root, bool indexHashes, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(root)) return;
            foreach (string path in Directory.EnumerateDirectories(root).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || LocalAsset(path, InfoFileName) == null) continue;
                    var entry = CustomLevelInfoWrapperLoader.LoadCustomLevelInfoWrapper(path);
                    if (entry == null || entry.customLevelInfo.difficultyLevels.Length == 0 ||
                        LocalAsset(entry.path, entry.customLevelInfo.difficultyLevels[0].audioPath) == null) continue;

                    _byPath[entry.levelID] = entry;
                    if (!indexHashes) continue;

                    string? hash = ComputeHash(entry);
                    if (hash != null && !_byHash.ContainsKey(hash)) _byHash[hash] = entry;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    // An invalid song folder does not prevent other entries from resolving.
                }
            }
        }

        private static string? ComputeHash(CustomLevelInfoWrapper entry)
        {
            string? info = LocalAsset(entry.path, InfoFileName);
            if (info == null) return null;
            using var hash = SHA1.Create();
            byte[] buffer = new byte[8192];
            AppendFile(info);
            foreach (CustomLevelInfo.DifficultyLevelInfo difficulty in entry.customLevelInfo.difficultyLevels)
            {
                string? path = LocalAsset(entry.path, difficulty?.jsonPath);
                if (path == null) return null;
                AppendFile(path);
            }
            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(hash.Hash).Replace("-", string.Empty);

            void AppendFile(string path)
            {
                using Stream input = File.OpenRead(path);
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
                    hash.TransformBlock(buffer, 0, count, buffer, 0);
            }
        }
    }
}
#endif
