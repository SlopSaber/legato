#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
#nullable enable

namespace Legato.Beatmaps
{
    using System;
    using System.Runtime.CompilerServices;

    internal static class EarlyBeatmapSources
    {
        private const string DomainKey = "Legato.Beatmaps.EarlyBeatmapSources";
#if !LEGATO_EARLY_STANDARD_MAPS
        private static readonly ConditionalWeakTable<BeatmapDataSO, CustomLevelInfo.DifficultyLevelInfo> Sources =
            GetShared<CustomLevelInfo.DifficultyLevelInfo>(DomainKey);
#else
        private static readonly ConditionalWeakTable<BeatmapDataSO, string> Files =
            GetShared<string>(DomainKey + ".ChartFiles");
#endif

        private static readonly ConditionalWeakTable<BeatmapDataSO, Tuple<string?, string?>> ImportedMetadata =
            GetShared<Tuple<string?, string?>>(DomainKey + ".ImportedMetadata");

#if !LEGATO_EARLY_STANDARD_MAPS
        internal static void Associate(BeatmapDataSO chart, CustomLevelInfo.DifficultyLevelInfo source, string? levelData = null, string? difficultyData = null)
        {
            Sources.GetValue(chart, _ => source);
            AssociateMetadata(chart, levelData, difficultyData);
        }

        internal static CustomLevelInfo.DifficultyLevelInfo? GetDifficulty(BeatmapDataSO chart) =>
            Sources.TryGetValue(chart, out CustomLevelInfo.DifficultyLevelInfo source) ? source : null;
#else
        internal static void AssociateFile(BeatmapDataSO chart, string file) => Files.GetValue(chart, _ => file);

        internal static string? GetFile(BeatmapDataSO chart) => Files.TryGetValue(chart, out string file) ? file : null;
#endif

        internal static void AssociateMetadata(BeatmapDataSO chart, string? levelData, string? difficultyData)
        {
            if (levelData != null || difficultyData != null)
            {
                ImportedMetadata.GetValue(chart, _ => Tuple.Create(levelData, difficultyData));
            }
        }

        internal static Tuple<string?, string?>? GetImportedMetadata(BeatmapDataSO chart) =>
            ImportedMetadata.TryGetValue(chart, out Tuple<string?, string?> metadata) ? metadata : null;

        private static ConditionalWeakTable<BeatmapDataSO, TValue> GetShared<TValue>(string key)
            where TValue : class
        {
            // Legato embeds a copy in each consumer. Keep one weak table keyed by native chart identity.
            AppDomain domain = AppDomain.CurrentDomain;
            lock (string.Intern(key))
            {
                if (domain.GetData(key) is ConditionalWeakTable<BeatmapDataSO, TValue> sources)
                {
                    return sources;
                }

                sources = new();
                domain.SetData(key, sources);
                return sources;
            }
        }
    }
}
#endif
