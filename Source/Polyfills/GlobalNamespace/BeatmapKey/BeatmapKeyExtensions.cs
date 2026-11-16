#nullable enable

using System.Linq;

namespace Legato {
    internal static class BeatmapKeyExtensions {
        internal static string LevelId(this BeatmapKey key) =>
#if BEAT_SABER_1_29_0 && LEGATO_NATIVE_BEATMAP_KEYS
            key?.level?.levelID ?? string.Empty;
#else
            key.levelId;
#endif

        internal static bool IsValidKey(this BeatmapKey key) =>
#if BEAT_SABER_1_29_0 && LEGATO_NATIVE_BEATMAP_KEYS
            key?.level != null && key.parentDifficultyBeatmapSet?.beatmapCharacteristic != null;
#else
            key.IsValid();
#endif

        internal static string FirstMapperName(this BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level?.levelAuthorName ?? string.Empty;
#else
            level?.allMappers?.FirstOrDefault() ?? string.Empty;
#endif

        internal static string CharacteristicWireName(this BeatmapKey key) => CharacteristicName(key) switch {
            "90Degree" => "Degree90",
            "360Degree" => "Degree360",
            var name => name
        };

        internal static string CharacteristicSerializedName(this BeatmapKey beatmapKey) {
#if BEAT_SABER_1_29_0 && LEGATO_NATIVE_BEATMAP_KEYS
            return beatmapKey?.parentDifficultyBeatmapSet?.beatmapCharacteristic?.serializedName ?? string.Empty;
#elif BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            return beatmapKey.beatmapCharacteristic?.serializedName ?? string.Empty;
#else
            return beatmapKey.characteristic.SerializedName();
#endif
        }

        internal static string CharacteristicName(this BeatmapKey beatmapKey) {
#if BEAT_SABER_1_29_0 && LEGATO_NATIVE_BEATMAP_KEYS
            return beatmapKey?.parentDifficultyBeatmapSet?.beatmapCharacteristic?.serializedName ?? string.Empty;
#elif BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            return beatmapKey.beatmapCharacteristic?.serializedName ?? string.Empty;
#else
            return beatmapKey.characteristic.ToString();
#endif
        }

        internal static bool Is360Degree(this BeatmapKey beatmapKey) {
#if BEAT_SABER_1_29_0 && LEGATO_NATIVE_BEATMAP_KEYS
            return beatmapKey?.parentDifficultyBeatmapSet?.beatmapCharacteristic?.serializedName == "360Degree";
#elif BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            return beatmapKey.beatmapCharacteristic?.serializedName == "360Degree";
#else
            return beatmapKey.characteristic == BeatmapCharacteristic.Degree360;
#endif
        }

        internal static bool CharacteristicMatches(this BeatmapKey beatmapKey, string name) {
#if BEAT_SABER_1_29_0 && LEGATO_NATIVE_BEATMAP_KEYS
            var characteristic = beatmapKey?.parentDifficultyBeatmapSet?.beatmapCharacteristic;
            return characteristic != null && (name == characteristic.serializedName || name == characteristic.characteristicNameLocalizationKey);
#elif BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            return name == beatmapKey.beatmapCharacteristic?.serializedName
                || name == beatmapKey.beatmapCharacteristic?.characteristicNameLocalizationKey;
#else
            return name == beatmapKey.characteristic.SerializedName()
                || name == beatmapKey.characteristic.NameLocalizationKey();
#endif
        }

        internal static string GetEnvironmentName(this BeatmapLevel beatmapLevel, in BeatmapKey beatmapKey) =>
#if BEAT_SABER_1_29_0 && LEGATO_NATIVE_BEATMAP_KEYS
            beatmapKey?.GetEnvironmentInfo()?.serializedName ?? string.Empty;
#elif BEAT_SABER_1_29_0
            beatmapKey.difficultyBeatmap.GetEnvironmentInfo()?.serializedName ?? string.Empty;
#elif BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            beatmapLevel.GetEnvironmentName(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
#elif BEAT_SABER_1_45_3
            beatmapLevel.GetEnvironmentName(in beatmapKey);
#else
            beatmapLevel.GetEnvironmentName(beatmapKey.characteristic, beatmapKey.difficulty);
#endif

#if !BEAT_SABER_1_29_0
        internal static bool TryGetDifficultyBeatmapData(this BeatmapLevel level, in BeatmapKey key, out BeatmapBasicData data) =>
#if BEAT_SABER_1_45_3
            level.gameplayTypeBeatmapBasicData.TryGetValue((key.gameplayType, key.characteristic, key.difficulty), out data);
#elif BEAT_SABER_1_45_1
            level.beatmapBasicData.TryGetValue((key.characteristic, key.difficulty), out data);
#else
            level.beatmapBasicData.TryGetValue((key.beatmapCharacteristic, key.difficulty), out data);
#endif

        internal static BeatmapBasicData? GetDifficultyBeatmapData(this BeatmapLevel beatmapLevel, in BeatmapKey beatmapKey) {
#if BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            return beatmapLevel.GetDifficultyBeatmapData(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
#elif BEAT_SABER_1_45_3
            return beatmapLevel.GetDifficultyBeatmapData(in beatmapKey);
#else
            return beatmapLevel.GetDifficultyBeatmapData(beatmapKey.characteristic, beatmapKey.difficulty);
#endif
        }
#endif

        internal static ColorScheme? GetColorScheme(this BeatmapLevel beatmapLevel, in BeatmapKey beatmapKey) {
#if BEAT_SABER_1_29_0
            return null;
#elif BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            return beatmapLevel.GetColorScheme(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty);
#elif BEAT_SABER_1_45_3
            return beatmapLevel.GetColorScheme(in beatmapKey);
#else
            return beatmapLevel.GetColorScheme(beatmapKey.characteristic, beatmapKey.difficulty);
#endif
        }
    }
}
