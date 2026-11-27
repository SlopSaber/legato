#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Legato.Beatmaps
{
    internal static class EarlyBeatmapSources
    {
        private const string DomainKey = "Legato.Beatmaps.EarlyBeatmapSources";
        private static readonly ConditionalWeakTable<BeatmapDataSO, CustomLevelInfo.DifficultyLevelInfo> Sources = GetSharedSources();

        internal static void Associate(BeatmapDataSO chart, CustomLevelInfo.DifficultyLevelInfo source) =>
            Sources.GetValue(chart, _ => source);

        internal static CustomLevelInfo.DifficultyLevelInfo? GetDifficulty(BeatmapDataSO chart) =>
            Sources.TryGetValue(chart, out CustomLevelInfo.DifficultyLevelInfo source) ? source : null;

        private static ConditionalWeakTable<BeatmapDataSO, CustomLevelInfo.DifficultyLevelInfo> GetSharedSources()
        {
            // Legato embeds a copy in each consumer. Keep one weak table keyed by native chart identity.
            AppDomain domain = AppDomain.CurrentDomain;
            lock (string.Intern(DomainKey))
            {
                if (domain.GetData(DomainKey) is ConditionalWeakTable<BeatmapDataSO, CustomLevelInfo.DifficultyLevelInfo> sources)
                {
                    return sources;
                }

                sources = new();
                domain.SetData(DomainKey, sources);
                return sources;
            }
        }
    }
}
#endif
