#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
namespace Vivify;

internal sealed class LegacyVivifyBeatClock
{
    private LegacyVivifyBeatClock(GameplayCoreSceneSetupData level)
    {
        StartingBpm = level.difficultyBeatmap.level.beatsPerMinute;
    }

    internal float StartingBpm { get; }
}
#endif
