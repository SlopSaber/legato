#if BEAT_SABER_1_29_0
using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using Zenject;
namespace SongCore.Hooks
{
    internal sealed class CustomCharacteristicsHook : IInitializable, IDisposable
    {
        private Hook? _lookup;
        public void Initialize() => _lookup = new Hook(typeof(BeatmapCharacteristicCollectionSO).GetMethod(nameof(BeatmapCharacteristicCollectionSO.GetBeatmapCharacteristicBySerializedName))!, Lookup, true);
        public void Dispose() => _lookup?.Dispose();
        private BeatmapCharacteristicSO? Lookup(Func<BeatmapCharacteristicCollectionSO, string, BeatmapCharacteristicSO> original, BeatmapCharacteristicCollectionSO collection, string name)
        {
            return original(collection, name) ?? Collections.customCharacteristics.FirstOrDefault(value => value.serializedName == name)
                ?? Collections.customCharacteristics.FirstOrDefault(value => value.serializedName == "MissingCharacteristic");
        }
    }
}
#endif
