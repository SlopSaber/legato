#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Legato.Beatmaps {
    internal static class LevelMedia {
        internal static BeatmapLevelsEntitlementModel Entitlements(BeatmapLevelsModel model) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            model._additionalContentModel;
#else
            model._entitlements;
#endif
        internal static IEnumerable<BeatmapLevel> OfficialLevels(BeatmapLevelsModel model) =>
#if LEGATO_BEFORE_MULTIPLAYER
            model.ostAndExtrasPackCollection.beatmapLevelPacks.Concat(model.dlcBeatmapLevelPackCollection.beatmapLevelPacks)
                .SelectMany(pack => pack.beatmapLevelCollection.beatmapLevels);
#elif BEAT_SABER_1_29_0
            model.allLoadedBeatmapLevelWithoutCustomLevelPackCollection.beatmapLevelPacks.SelectMany(pack => pack.beatmapLevelCollection.beatmapLevels);
#elif BEAT_SABER_1_37_1 && !LEGATO_ADDITIONAL_PACK_LEVELS
            model.ostAndExtrasBeatmapLevelsRepository.beatmapLevelPacks.Concat(model.dlcBeatmapLevelsRepository.beatmapLevelPacks).SelectMany(pack => pack.beatmapLevels);
#else
            model.ostAndExtrasBeatmapLevelsRepository.beatmapLevelPacks.Concat(model.dlcBeatmapLevelsRepository.beatmapLevelPacks).SelectMany(pack => pack._beatmapLevels);
#endif
#if LEGATO_BEFORE_MULTIPLAYER
        internal static Task<AudioClip> PreviewAudio(BeatmapLevelsModel model, BeatmapLevel level, CancellationToken token) {
            var loaded = level as IBeatmapLevel ?? model.GetBeatmapLevelIfLoaded(level.levelID);
            var audio = loaded?.beatmapLevelData?.audioClip;
            return audio != null ? Task.FromResult(audio) : level.GetPreviewAudioClipAsync(token);
        }
#else
        internal static Task<AudioClip>? CachedAudio(BeatmapLevelsModel model, AudioClipAsyncLoader loader, string levelId) {
#if BEAT_SABER_1_29_0
            var level = model.GetBeatmapLevelIfLoaded(levelId);
            return level == null ? null : loader.LoadSong(level);
#else
            var levels = (BeatmapLevelLoader)model.levelLoader;
            return levels._loadedBeatmapLevelDataCache.TryGetFromCache(levelId, out var data) ? loader.LoadSong(data) : null;
#endif
        }
#endif
    }
}
