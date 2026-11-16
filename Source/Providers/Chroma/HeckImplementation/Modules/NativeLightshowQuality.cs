#if BEAT_SABER_1453
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Chroma.Settings;
using SiraUtil.Affinity;

namespace Chroma.Modules;

internal class NativeLightshowQuality : IAffinity
{
    private readonly Config _config;
    private readonly BeatmapLevelLoader _levelLoader;
    private readonly ConditionalWeakTable<IBeatmapLevelData, Dictionary<BeatmapKey, (int Version, bool Quality)>> _cache = new();

    private NativeLightshowQuality(Config config, BeatmapLevelLoader levelLoader)
    {
        _config = config;
        _levelLoader = levelLoader;
    }

    internal bool IsQuality(BeatmapLevel beatmapLevel, in BeatmapKey beatmapKey)
    {
        if (beatmapLevel.previewMediaData is not StaticPreviewMediaData)
        {
            return false;
        }

        try
        {
            if (!_levelLoader._loadedBeatmapLevelDataCache.TryGetFromCache(beatmapLevel.levelID, out IBeatmapLevelData? levelData) ||
                levelData == null ||
                !levelData.ContainsBeatmapData(in beatmapKey))
            {
                Plugin.Log.Debug($"Native lightshow data for [{beatmapKey}] is not loaded; retaining the environment override.");
                return false;
            }

            Dictionary<BeatmapKey, (int Version, bool Quality)> cache = _cache.GetOrCreateValue(levelData);
            if (cache.TryGetValue(beatmapKey, out (int Version, bool Quality) cached) && cached.Version == levelData.version)
            {
                return cached.Quality;
            }

            // A separate lightshow is authoritative; older assets can embed lighting in the beatmap.
            string? text = levelData.GetLightshowString(in beatmapKey) ?? levelData.GetBeatmapString(in beatmapKey);
            if (text == null)
            {
                return false;
            }

            bool quality = LightshowQuality.HasQualityLights(text);
            cache[beatmapKey] = (levelData.version, quality);
            Plugin.Log.Debug($"Native lightshow [{beatmapKey}] {(quality ? "meets" : "does not meet")} the quality threshold.");
            return quality;
        }
        catch (Exception ex)
        {
            Plugin.Log.Debug($"Could not classify the native lightshow [{beatmapKey}]: {ex.Message}");
            return false;
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(BeatmapLevelLoader), nameof(BeatmapLevelLoader.CheckBeatmapLevelDataExistsAsync))]
    private void PrepareNativeLevel(
        BeatmapLevelLoader __instance,
        BeatmapLevel beatmapLevel,
        BeatmapLevelDataVersion beatmapLevelDataVersion,
        CancellationToken cancellationToken,
        ref Task<bool> __result)
    {
        if (!_config.ForceMapEnvironmentWhenQualityLights ||
            !ReferenceEquals(__instance, _levelLoader) ||
            beatmapLevel.previewMediaData is not StaticPreviewMediaData ||
            _levelLoader._loadedBeatmapLevelDataCache.IsInCache(beatmapLevel.levelID))
        {
            return;
        }

        // The menu awaits this check before enabling Play. Warm native assets within that async
        // boundary so environment selection never waits synchronously for an asset load.
        __result = PrepareNativeLevelAsync(__result, beatmapLevel, beatmapLevelDataVersion, cancellationToken);
    }

    private async Task<bool> PrepareNativeLevelAsync(
        Task<bool> availability,
        BeatmapLevel beatmapLevel,
        BeatmapLevelDataVersion beatmapLevelDataVersion,
        CancellationToken cancellationToken)
    {
        bool exists = await availability;
        if (!exists || !_config.ForceMapEnvironmentWhenQualityLights)
        {
            return exists;
        }

        try
        {
            LoadBeatmapLevelDataResult result = await _levelLoader.LoadBeatmapLevelDataAsync(
                beatmapLevel,
                beatmapLevelDataVersion,
                cancellationToken);
            if (result.isError)
            {
                Plugin.Log.Debug($"Could not prepare native lightshow [{beatmapLevel.levelID}]: {result.errorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Plugin.Log.Debug($"Could not prepare native lightshow [{beatmapLevel.levelID}]: {ex.Message}");
        }

        return exists;
    }
}
#endif
