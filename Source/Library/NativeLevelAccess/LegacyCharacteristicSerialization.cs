#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
namespace Legato.Beatmaps {
    internal static class LegacyCharacteristicSerialization {
        internal static string SerializedName(this BeatmapCharacteristicSO characteristic) => characteristic.serializedName;
    }
}
#endif
