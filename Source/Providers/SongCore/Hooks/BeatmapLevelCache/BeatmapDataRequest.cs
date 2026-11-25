#if !BEAT_SABER_1_29_0
namespace SongCore.Hooks.BeatmapLevelCache
{
    internal record BeatmapDataRequest(IBeatmapLevelData BeatmapLevelData, BeatmapKey BeatmapKey, float StartBpm, bool LoadingForDesignatedEnvironment, IEnvironmentInfo? TargetEnvironmentInfo, IEnvironmentInfo? OriginalEnvironmentInfo, BeatmapLevelDataVersion BeatmapLevelDataVersion, GameplayModifiers? GameplayModifiers, PlayerSpecificSettings? PlayerSpecificSettings, bool EnableNativeCaching);
}

#endif
