#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps
{
    internal sealed class EarlyLevelCatalog
    {
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

            string root = Path.GetFullPath(CustomLevelsModelSO.customLevelsDirectoryPath);
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
            string root = Path.GetFullPath(CustomLevelsModelSO.customLevelsDirectoryPath);
            var entry = EarlyMapFiles.ReadLocal(root, level?.levelID);
            string? path = entry == null ? null : EarlyMapFiles.LocalAsset(entry.path, entry.customLevelInfo.coverImagePath);
            return path == null ? null : File.OpenRead(path);
        }

        private void ReadCustomLevels(string root, bool indexHashes, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(root)) return;
            foreach (string path in Directory.EnumerateDirectories(root).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var entry = EarlyMapFiles.ReadLocal(root, path);
                    if (entry == null || entry.customLevelInfo.difficultyLevels.Length == 0 ||
                        EarlyMapFiles.LocalAsset(entry.path, entry.customLevelInfo.difficultyLevels[0].audioPath) == null) continue;

                    _byPath[entry.levelID] = entry;
                    if (!indexHashes) continue;

                    string? hash = EarlyMapFiles.ComputeHash(entry);
                    if (hash != null && !_byHash.ContainsKey(hash)) _byHash[hash] = entry;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    // An invalid song folder does not prevent other entries from resolving.
                }
            }
        }

    }
}
#endif
