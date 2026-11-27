#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;

namespace Legato.Beatmaps;

internal static class EarlyMapFiles
{
    internal const string InfoFileName = "info.json";

    internal static CustomLevelInfoWrapper? ReadLocal(string root, string? levelId)
    {
        if (string.IsNullOrEmpty(levelId) || !Path.IsPathRooted(levelId)) return null;
        string directory = Path.GetFullPath(levelId);
        if (!string.Equals(Path.GetDirectoryName(directory), root, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0 ||
            LocalAsset(directory, InfoFileName) == null) return null;

        return CustomLevelInfoWrapperLoader.LoadCustomLevelInfoWrapper(directory);
    }

    internal static string? FindHash(string root, string levelId)
    {
        try
        {
            CustomLevelInfoWrapper? entry = ReadLocal(root, levelId);
            return entry == null ? null : ComputeHash(entry);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
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

    internal static string? ComputeHash(CustomLevelInfoWrapper entry)
    {
        string? info = LocalAsset(entry.path, InfoFileName);
        if (info == null || entry.customLevelInfo.difficultyLevels.Length == 0) return null;
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
#endif
