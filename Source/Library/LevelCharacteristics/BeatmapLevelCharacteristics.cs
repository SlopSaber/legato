#nullable enable
using System.Collections.Generic;

namespace Legato.Beatmaps {
    internal static class BeatmapLevelCharacteristics {
#if BEAT_SABER_1_45_3
        internal static IEnumerable<BeatmapCharacteristic> GetBeatSaber(BeatmapLevel level) =>
            level.GetCharacteristics(GameplayType.BeatSaber);
#elif BEAT_SABER_1_45_1
        internal static IEnumerable<BeatmapCharacteristic> GetBeatSaber(BeatmapLevel level) =>
            level.GetCharacteristics();
#else
        internal static IEnumerable<BeatmapCharacteristicSO> GetBeatSaber(BeatmapLevel level) =>
            level.GetCharacteristics();
#endif
    }
}
