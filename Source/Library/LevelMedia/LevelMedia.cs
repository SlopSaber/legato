#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps {
    internal static class LevelMedia {
        internal static BeatmapLevelsEntitlementModel Entitlements(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0
            model._additionalContentModel;
#else
            model._entitlements;
#endif
        internal static IEnumerable<BeatmapLevel> OfficialLevels(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0
            model.allLoadedBeatmapLevelWithoutCustomLevelPackCollection.beatmapLevelPacks.SelectMany(pack => pack.beatmapLevelCollection.beatmapLevels);
#else
            model.ostAndExtrasBeatmapLevelsRepository.beatmapLevelPacks.Concat(model.dlcBeatmapLevelsRepository.beatmapLevelPacks).SelectMany(pack => pack._beatmapLevels);
#endif
        internal static Task<AudioClip>? CachedAudio(BeatmapLevelsModel model, AudioClipAsyncLoader loader, string levelId) {
#if BEAT_SABER_1_29_0
            var level = model.GetBeatmapLevelIfLoaded(levelId);
            return level == null ? null : loader.LoadSong(level);
#else
            var levels = (BeatmapLevelLoader)model.levelLoader;
            return levels._loadedBeatmapLevelDataCache.TryGetFromCache(levelId, out var data) ? loader.LoadSong(data) : null;
#endif
        }
    }
}
