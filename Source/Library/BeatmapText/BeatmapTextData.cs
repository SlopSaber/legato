#nullable enable
#if !LEGATO_DIFFICULTY_BEATMAPS
using System.Threading;
using System.Threading.Tasks;

namespace Legato.Beatmaps {
    internal static class BeatmapTextData {
        internal static Task<BeatmapLevelDataVersion> GetVersion(BeatmapLevelsEntitlementModel model, string levelId, CancellationToken token) =>
            model.GetLevelDataVersionAsync(levelId, token);

        internal static Task<LoadBeatmapLevelDataResult> Load(BeatmapLevelsModel model, string levelId, BeatmapLevelDataVersion version, CancellationToken token) =>
            model.LoadBeatmapLevelDataAsync(levelId, version, token);

        internal static bool HasData(LoadBeatmapLevelDataResult result) => !result.isError && result.beatmapLevelData != null;
        internal static string? Error(LoadBeatmapLevelDataResult result) =>
#if LEGATO_BEATMAP_LOAD_ERROR_MESSAGE
            result.errorMessage;
#else
            result.isError ? "The game could not load beatmap data." : null;
#endif

        internal static Task<string?> ReadText(LoadBeatmapLevelDataResult result, in BeatmapKey key) =>
            result.beatmapLevelData!.GetBeatmapStringAsync(in key);
    }
}
#endif
