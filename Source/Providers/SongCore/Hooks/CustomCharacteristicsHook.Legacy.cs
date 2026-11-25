#if LEGATO_SCRIPTABLE_CHARACTERISTICS
using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using Zenject;
namespace SongCore.Hooks
{
    internal sealed class CustomCharacteristicsHook : IInitializable, IDisposable
    {
        private Hook? _lookup;
        public void Initialize() => _lookup = new Hook(typeof(BeatmapCharacteristicCollection).GetMethod(nameof(BeatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName))!, Lookup, true);
        public void Dispose() => _lookup?.Dispose();
        private BeatmapCharacteristicSO? Lookup(Func<BeatmapCharacteristicCollection, string, BeatmapCharacteristicSO> original, BeatmapCharacteristicCollection collection, string name)
        {
            return original(collection, name) ?? Collections.customCharacteristics.FirstOrDefault(value => value.serializedName == name)
                ?? Collections.customCharacteristics.FirstOrDefault(value => value.serializedName == "MissingCharacteristic");
        }
    }
}
#endif
