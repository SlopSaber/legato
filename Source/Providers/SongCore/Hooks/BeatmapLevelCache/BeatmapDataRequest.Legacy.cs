#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
#nullable enable
namespace SongCore.Hooks.BeatmapLevelCache
{
    internal sealed record BeatmapDataRequest(IDifficultyBeatmap Difficulty, EnvironmentInfoSO Environment, PlayerSpecificSettings PlayerSettings);
}
#endif
