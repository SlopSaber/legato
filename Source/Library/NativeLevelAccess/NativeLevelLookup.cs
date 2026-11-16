using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Legato.Beatmaps {
    internal static class NativeLevelLookup {
        internal static async Task<BeatmapLevel> ByIdOrHash(BeatmapLevelsModel model, string hash, CancellationToken token) {
#if BEAT_SABER_1_29_0
            var preview = model.GetLevelPreviewForLevelId(hash) ?? model.GetLevelPreviewForLevelId(CustomLevelLoader.kCustomLevelPrefixId + hash);
            if (preview == null && hash.Length == 40) {
                var customId = CustomLevelLoader.kCustomLevelPrefixId + hash;
                preview = model.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks
                    .SelectMany(pack => pack.beatmapLevelCollection.beatmapLevels)
                    .FirstOrDefault(level => level.levelID.StartsWith(hash, System.StringComparison.OrdinalIgnoreCase) || level.levelID.StartsWith(customId, System.StringComparison.OrdinalIgnoreCase));
            }
            if (preview == null) return null;
            var loaded = await model.GetBeatmapLevelAsync(preview.levelID, token).ConfigureAwait(true);
            return loaded.isError ? null : loaded.beatmapLevel;
#else
            if (hash.Length == 40) {
                var fixedHash = model._allLoadedBeatmapLevelsRepository._idToBeatmapLevel.Keys.FirstOrDefault(key => key.StartsWith(hash, System.StringComparison.Ordinal));
                if (fixedHash != null) hash = fixedHash;
            }
            if (await model.CheckBeatmapLevelDataExistsAsync(hash, BeatmapLevelDataVersion.Original, token).ConfigureAwait(true))
                return model.GetBeatmapLevel(hash);
            if (await model.CheckBeatmapLevelDataExistsAsync(CustomLevelLoader.kCustomLevelPrefixId + hash, BeatmapLevelDataVersion.Original, token).ConfigureAwait(true))
                return model.GetBeatmapLevel(CustomLevelLoader.kCustomLevelPrefixId + hash);
            return null;
#endif
        }
    }
}
