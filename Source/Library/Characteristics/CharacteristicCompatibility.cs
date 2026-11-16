#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Legato.Compatibility {
    internal static class CharacteristicCompatibility {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private static BeatmapCharacteristicCollectionSO? _collection;
        private static readonly Dictionary<BeatmapCharacteristic, BeatmapCharacteristicSO> _assets = new();

        private static BeatmapCharacteristicSO? Resolve(BeatmapCharacteristic characteristic) {
            if (_assets.TryGetValue(characteristic, out BeatmapCharacteristicSO cached) && cached != null) return cached;
            if (_collection == null) _collection = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicCollectionSO>().FirstOrDefault();
            BeatmapCharacteristicSO? asset = _collection?.allBeatmapCharacteristics?
                .FirstOrDefault(candidate => BeatmapCharacteristicExtensions.AsEnum(candidate) == characteristic)
                ?? Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>()
                    .FirstOrDefault(candidate => BeatmapCharacteristicExtensions.AsEnum(candidate) == characteristic);
            if (asset != null) _assets[characteristic] = asset;
            return asset;
        }
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

        internal static BeatmapCharacteristicSO? GetAsset(global::BeatmapKey key) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return Resolve(key.characteristic);
#else
            return key.beatmapCharacteristic;
#endif
        }

        internal static object GetNativeValue(global::BeatmapKey key) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return key.characteristic;
#else
            return key.beatmapCharacteristic;
#endif
        }

        internal static global::BeatmapKey CreateKey(string levelId, BeatmapCharacteristicSO asset, BeatmapDifficulty difficulty) {
#if BEAT_SABER_1_45_3
            return new global::BeatmapKey(levelId, GameplayType.BeatSaber, FromAsset(asset), difficulty);
#else
            return new global::BeatmapKey(levelId, FromAsset(asset), difficulty);
#endif
        }
    }
}
