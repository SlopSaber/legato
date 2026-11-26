#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Legato.Beatmaps {
    internal static class BeatmapLevelDataCopy {
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
#if BEAT_SABER_1_45_3
        internal static Dictionary<(GameplayType, BeatmapCharacteristic, BeatmapDifficulty), BeatmapBasicData> CopyBasicData(BeatmapLevel source) =>
            source.gameplayTypeBeatmapBasicData.ToDictionary(pair => pair.Key, pair => pair.Value);
#elif !LEGATO_SCRIPTABLE_CHARACTERISTICS
        internal static Dictionary<(BeatmapCharacteristic, BeatmapDifficulty), BeatmapBasicData> CopyBasicData(BeatmapLevel source) =>
            source.beatmapBasicData.ToDictionary(pair => pair.Key, pair => pair.Value);
#else
        internal static Dictionary<(BeatmapCharacteristicSO, BeatmapDifficulty), BeatmapBasicData> CopyBasicData(BeatmapLevel source) =>
            source.beatmapBasicData.ToDictionary(pair => pair.Key, pair => pair.Value);
#endif
#endif
    }
}
