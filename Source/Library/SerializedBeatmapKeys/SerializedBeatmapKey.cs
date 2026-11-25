#if !BEAT_SABER_1_29_0 && !BEAT_SABER_1_34_2
using System;
#if !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endif

namespace Legato.Gameplay
{
    internal static class SerializedBeatmapKey
    {
#if !BEAT_SABER_1_45_1 && !BEAT_SABER_1_45_3
        private static readonly Dictionary<string, BeatmapCharacteristicSO> Assets = new(StringComparer.OrdinalIgnoreCase);
#endif

        internal static BeatmapKey Create(string levelId, string serializedName, BeatmapDifficulty difficulty)
        {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            if (!Enum.TryParse(serializedName, true, out BeatmapCharacteristic characteristic))
            {
                characteristic = serializedName.ToLowerInvariant() switch
                {
                    "360degree" => BeatmapCharacteristic.Degree360,
                    "90degree" => BeatmapCharacteristic.Degree90,
                    _ => throw new InvalidOperationException("Failed to find characteristic: " + serializedName),
                };
            }
#if BEAT_SABER_1_45_3
            return new BeatmapKey(levelId, GameplayType.BeatSaber, characteristic, difficulty);
#else
            return new BeatmapKey(levelId, characteristic, difficulty);
#endif
#else
            if (!Assets.TryGetValue(serializedName, out var asset) || asset == null)
            {
                asset = UnityEngine.Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>().FirstOrDefault(
                    value => string.Equals(value.serializedName, serializedName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Failed to find characteristic: " + serializedName);
                Assets[serializedName] = asset;
            }

            return new BeatmapKey(levelId, asset, difficulty);
#endif
        }
    }
}
#endif
