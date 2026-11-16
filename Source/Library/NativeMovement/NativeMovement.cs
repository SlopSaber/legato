#if BEAT_SABER_1_29_0
global using VariableMovementDataProvider = global::BeatmapObjectSpawnMovementData;
global using IVariableMovementDataProvider = global::BeatmapObjectSpawnMovementData;
global using NoteSpawnData = global::BeatmapObjectSpawnMovementData.NoteSpawnData;
#endif

namespace Legato.Gameplay {
    internal static class NativeMovement {
        internal static float HalfJumpDuration(IVariableMovementDataProvider movement) =>
#if BEAT_SABER_1_29_0
            movement.jumpDuration * 0.5f;
#else
            movement.halfJumpDuration;
#endif
    }
}
