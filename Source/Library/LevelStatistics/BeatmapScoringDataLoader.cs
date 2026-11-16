#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Legato.Beatmaps {
    internal sealed class BeatmapScoringDataLoader {
#if !BEAT_SABER_1_29_0
        private readonly BeatmapLevelsModel _levels;
        private readonly BeatmapLevelsEntitlementModel _entitlements;
        private readonly BeatmapDataLoader _loader;
        public BeatmapScoringDataLoader(BeatmapLevelsModel levels, BeatmapLevelsEntitlementModel entitlements, BeatmapDataLoader loader) {
            _levels = levels;
            _entitlements = entitlements;
            _loader = loader;
        }
#endif
        internal async Task<IReadonlyBeatmapData> Load(BeatmapKey key, BeatmapLevel? level, CancellationToken token) {
            token.ThrowIfCancellationRequested();
#if BEAT_SABER_1_29_0
            // Load through the native difficulty so custom JSON and shared v4 conversion hooks remain active.
            var data = await key.GetBeatmapDataAsync(key.GetEnvironmentInfo(), null);
#else
            level ??= _levels.GetBeatmapLevel(key.levelId);
            if (level == null) throw new InvalidOperationException("Failed to get BeatmapLevel.");
            var version = await _entitlements.GetLevelDataVersionAsync(key.levelId, token);
            token.ThrowIfCancellationRequested();
            var result = await _levels.LoadBeatmapLevelDataAsync(key.levelId, version, token);
            if (result.isError || result.beatmapLevelData == null)
                throw new InvalidOperationException("Failed to load beat map level data.");
            token.ThrowIfCancellationRequested();
            var data = await _loader.LoadBeatmapDataAsync(result.beatmapLevelData, key, level.beatsPerMinute, false,
#if BEAT_SABER_1_37_1
                null,
#else
                null, null,
#endif
                version, null, null
#if !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
                , false
#endif
            );
#endif
            token.ThrowIfCancellationRequested();
            return data ?? throw new InvalidOperationException("Failed to get BeatmapData.");
        }
    }
}
