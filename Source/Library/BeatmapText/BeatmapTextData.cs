#nullable enable
#if BEAT_SABER_1_45_3
using System.Threading;
using System.Threading.Tasks;

namespace Legato.Beatmaps {
    internal static class BeatmapTextData {
        internal static Task<BeatmapLevelDataVersion> GetVersion(BeatmapLevelsEntitlementModel model, string levelId, CancellationToken token) =>
            model.GetLevelDataVersionAsync(levelId, token);

        internal static Task<LoadBeatmapLevelDataResult> Load(BeatmapLevelsModel model, string levelId, BeatmapLevelDataVersion version, CancellationToken token) =>
            model.LoadBeatmapLevelDataAsync(levelId, version, token);

        internal static bool HasData(LoadBeatmapLevelDataResult result) => !result.isError && result.beatmapLevelData != null;
        internal static string? Error(LoadBeatmapLevelDataResult result) => result.errorMessage;

        internal static Task<string?> ReadText(LoadBeatmapLevelDataResult result, in BeatmapKey key) =>
            result.beatmapLevelData!.GetBeatmapStringAsync(in key);
    }
}
#endif
