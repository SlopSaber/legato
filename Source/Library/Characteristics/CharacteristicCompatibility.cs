#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Legato.Compatibility {
    internal static class CharacteristicCompatibility {
        internal static string SerializedName(NativeBeatmapCharacteristic characteristic) =>
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            characteristic.SerializedName();
#else
            characteristic.serializedName;
#endif

#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private static BeatmapCharacteristicCollectionSO? _collection;
        private static readonly Dictionary<BeatmapCharacteristic, BeatmapCharacteristicSO> _assets = new();

        private static BeatmapCharacteristicSO? Resolve(BeatmapCharacteristic characteristic) {
            if (_assets.TryGetValue(characteristic, out BeatmapCharacteristicSO cached) && cached != null) return cached;
            if (_collection == null) _collection = UnityEngine.Resources.FindObjectsOfTypeAll<BeatmapCharacteristicCollectionSO>().FirstOrDefault();
            BeatmapCharacteristicSO? asset = _collection?.allBeatmapCharacteristics?
                .FirstOrDefault(candidate => BeatmapCharacteristicExtensions.AsEnum(candidate) == characteristic)
                ?? UnityEngine.Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>()
                    .FirstOrDefault(candidate => BeatmapCharacteristicExtensions.AsEnum(candidate) == characteristic);
            if (asset != null) _assets[characteristic] = asset;
            return asset;
        }
#endif

        internal static string KeyCharacteristicMember =>
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            "characteristic";
#else
            "beatmapCharacteristic";
#endif

        internal static bool TryResolveEnum(object? value, out BeatmapCharacteristicSO? asset) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            if (value is BeatmapCharacteristic characteristic) {
                asset = Resolve(characteristic);
                return true;
            }
#endif
            asset = null;
            return false;
        }

        internal static bool UsesEnumStorage(Type owner) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return owner.GetField("<lastSelectedBeatmapCharacteristic>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.FieldType == typeof(BeatmapCharacteristic);
#else
            return false;
#endif
        }

        internal static NativeBeatmapCharacteristic FromAsset(BeatmapCharacteristicSO? asset) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return asset == null ? default : BeatmapCharacteristicExtensions.AsEnum(asset);
#else
            return asset!;
#endif
        }

        internal static BeatmapCharacteristicSO? GetAsset(NativeBeatmapKey key) {
#if BEAT_SABER_1_29_0
            return key.parentDifficultyBeatmapSet.beatmapCharacteristic;
#else
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return Resolve(key.characteristic);
#else
            return key.beatmapCharacteristic;
#endif
#endif
        }

        internal static object GetNativeValue(object value) {
#if BEAT_SABER_1_29_0
            return value is NativeBeatmapKey difficultyBeatmap
                ? difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic
                : ((LegacyBeatmapKey)value).beatmapCharacteristic;
#else
            var key = (NativeBeatmapKey)value;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return key.characteristic;
#else
            return key.beatmapCharacteristic;
#endif
#endif
        }

        internal static string KeyLevelId(object value) {
#if BEAT_SABER_1_29_0
            return value is NativeBeatmapKey difficultyBeatmap ? difficultyBeatmap.level.levelID : ((LegacyBeatmapKey)value).levelId;
#else
            return ((NativeBeatmapKey)value).levelId;
#endif
        }

        internal static BeatmapDifficulty KeyDifficulty(object value) {
#if BEAT_SABER_1_29_0
            return value is NativeBeatmapKey difficultyBeatmap ? difficultyBeatmap.difficulty : ((LegacyBeatmapKey)value).difficulty;
#else
            return ((NativeBeatmapKey)value).difficulty;
#endif
        }

        internal static object CreateKey(string levelId, BeatmapCharacteristicSO asset, BeatmapDifficulty difficulty) {
#if BEAT_SABER_1_29_0
            return new LegacyBeatmapKey(levelId, asset, difficulty);
#elif BEAT_SABER_1_45_3
            return new NativeBeatmapKey(levelId, GameplayType.BeatSaber, FromAsset(asset), difficulty);
#else
            return new NativeBeatmapKey(levelId, FromAsset(asset), difficulty);
#endif
        }
    }
}
