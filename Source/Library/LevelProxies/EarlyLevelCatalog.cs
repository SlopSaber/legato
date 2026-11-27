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
        private readonly Dictionary<string, EarlyMapEntry> _byPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EarlyMapEntry> _byHash = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _hashes = new(StringComparer.OrdinalIgnoreCase);

        private EarlyLevelCatalog(Dictionary<string, StandardLevelSO> loaded)
        {
            _loaded = loaded;
        }

        internal static BeatmapLevel? FindLoaded(string? levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            return UnityEngine.Resources.FindObjectsOfTypeAll<StandardLevelSO>()
                .FirstOrDefault(level => level && string.Equals(level.levelID, levelId, StringComparison.OrdinalIgnoreCase));
        }

        internal IReadOnlyCollection<EarlyMapEntry> Entries => _byPath.Values;

        internal bool TryGetEntry(string levelId, out EarlyMapEntry? entry) => _byPath.TryGetValue(levelId, out entry);

        internal string? HashFor(string levelId) => _hashes.TryGetValue(levelId, out string hash) ? hash : null;

        internal static Task<EarlyLevelCatalog> ReadAsync(bool indexHashes, CancellationToken cancellationToken) =>
            ReadAsync(new[] { CustomLevelsModelSO.customLevelsDirectoryPath }, indexHashes, cancellationToken);

        internal static Task<EarlyLevelCatalog> ReadAsync(IEnumerable<string> customRoots, bool indexHashes, CancellationToken cancellationToken)
        {
            var loaded = new Dictionary<string, StandardLevelSO>(StringComparer.OrdinalIgnoreCase);
            foreach (StandardLevelSO level in UnityEngine.Resources.FindObjectsOfTypeAll<StandardLevelSO>())
            {
                if (level && !string.IsNullOrEmpty(level.levelID)) loaded[level.levelID] = level;
            }

            string[] roots = customRoots.Where(root => !string.IsNullOrWhiteSpace(root)).ToArray();
            var catalog = new EarlyLevelCatalog(loaded);
            return Task.Run(() =>
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string root in roots)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        string directory = Path.GetFullPath(root);
                        if (directory.Length > Path.GetPathRoot(directory).Length)
                            directory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (visited.Add(directory)) catalog.ReadCustomLevels(directory, indexHashes, cancellationToken);
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                    {
                        // A configured folder can be unavailable while the native song folder remains readable.
                    }
                }
                return catalog;
            }, cancellationToken);
        }

        internal async Task<BeatmapLevel?> LoadAsync(string? levelId, string? hash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (levelId != null && _loaded.TryGetValue(levelId, out var loaded) && loaded) return loaded;

            EarlyMapEntry? info = null;
            if (levelId != null) _byPath.TryGetValue(levelId, out info);
            if (info == null && hash != null) _byHash.TryGetValue(hash, out info);
            if (info == null) return null;

            string nativeId = info.Native.levelID;
            if (!_loaded.TryGetValue(nativeId, out loaded) || !loaded)
            {
                loaded = await EarlyCustomLevelLoader.LoadAsync(info, cancellationToken);
                if (loaded) _loaded[nativeId] = loaded!;
            }
            return loaded;
        }

        internal static Stream? OpenCustomCover(BeatmapLevel? level)
        {
            string root = Path.GetFullPath(CustomLevelsModelSO.customLevelsDirectoryPath);
            var entry = EarlyMapFiles.ReadLocal(root, level?.levelID);
            string? path = entry == null ? null : EarlyMapFiles.LocalAsset(entry.Native.path, entry.Native.customLevelInfo.coverImagePath);
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
                    if (entry == null) continue;
                    CustomLevelInfoWrapper native = entry.Native;
                    if (EarlyMapFiles.LocalAsset(native.path, native.customLevelInfo.difficultyLevels[0]?.audioPath) == null) continue;

                    _byPath[native.levelID] = entry;
                    if (!indexHashes) continue;

                    string? hash = EarlyMapFiles.ComputeHash(entry);
                    if (hash != null)
                    {
                        _hashes[native.levelID] = hash;
                        if (!_byHash.ContainsKey(hash)) _byHash[hash] = entry;
                    }
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
