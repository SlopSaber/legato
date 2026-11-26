using CustomJSONData.CustomBeatmap;

namespace Vivify.Events;

internal static class EventTiming
{
    internal static float DurationInSeconds(float beats, CustomEventData customEventData, IBpmController clock)
    {
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        float bpm = customEventData.beatsPerMinute ?? clock.StartingBpm;
#else
        float bpm = clock.currentBpm;
#endif
        return (60f * beats) / bpm;
    }
}
