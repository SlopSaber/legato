#nullable enable

namespace Legato {
    // this property arrived in 1.38; the old one is the same data
    internal static class SaberExtensions {
        internal static SaberMovementData GetMovementDataForLogic(this Saber saber) =>
#if LEGATO_DIFFICULTY_BEATMAPS || BEAT_SABER_1_37_1
            saber.movementData;
#else
            saber.movementDataForLogic;
#endif
    }
}
