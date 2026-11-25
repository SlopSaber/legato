#if !LEGATO_VARIABLE_MOVEMENT_DATA
global using VariableMovementDataProvider = global::BeatmapObjectSpawnMovementData;
global using IVariableMovementDataProvider = global::BeatmapObjectSpawnMovementData;
global using NoteSpawnData = global::BeatmapObjectSpawnMovementData.NoteSpawnData;
#endif

namespace Legato.Gameplay {
    internal static class NativeMovement {
        internal static float HalfJumpDuration(IVariableMovementDataProvider movement) =>
#if !LEGATO_VARIABLE_MOVEMENT_DATA
            movement.jumpDuration * 0.5f;
#else
            movement.halfJumpDuration;
#endif
    }
}
