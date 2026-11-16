#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Legato.UI {
    internal static class MultiplayerResultsSelection {
        internal static Task<BeatmapKey> Resolve(object[] args, BeatmapLevelsModel levels, CancellationToken token) {
            token.ThrowIfCancellationRequested();
#if BEAT_SABER_1_29_0
            return ResolveLegacy((IPreviewBeatmapLevel)args[1], (BeatmapDifficulty)args[2], (BeatmapCharacteristicSO)args[3], levels, token);
#else
            return Task.FromResult((BeatmapKey)args[1]);
#endif
        }
#if BEAT_SABER_1_29_0
        private static async Task<BeatmapKey> ResolveLegacy(IPreviewBeatmapLevel preview, BeatmapDifficulty difficulty,
            BeatmapCharacteristicSO characteristic, BeatmapLevelsModel levels, CancellationToken token) {
            IBeatmapLevel? level = preview as IBeatmapLevel;
            if (level == null) {
                var result = await levels.GetBeatmapLevelAsync(preview.levelID, token);
                token.ThrowIfCancellationRequested();
                if (result.isError) throw new InvalidOperationException("Failed to load multiplayer result level.");
                level = result.beatmapLevel;
            }
            if (level != null) {
                foreach (var set in level.beatmapLevelData.difficultyBeatmapSets) {
                    if (set.beatmapCharacteristic.serializedName != characteristic.serializedName) continue;
                    foreach (var beatmap in set.difficultyBeatmaps)
                        if (beatmap.difficulty == difficulty) return beatmap;
                }
            }
            throw new InvalidOperationException("Multiplayer result difficulty is unavailable.");
        }
#endif
    }
}
