using System;

namespace Legato.Beatmaps {
    internal static class ReplayLaunchValues {
        internal static object Key(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key.difficultyBeatmap ?? throw new ArgumentException("Replay has no native difficulty beatmap.", nameof(key));
#else
            key;
#endif

        internal static object Level(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level.previewBeatmapLevel;
#else
            level;
#endif
    }
}
