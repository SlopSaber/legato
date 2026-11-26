#if !LEGATO_VARIABLE_MOVEMENT_DATA
global using VariableMovementDataProvider = global::BeatmapObjectSpawnMovementData;
global using IVariableMovementDataProvider = global::BeatmapObjectSpawnMovementData;
#if !LEGATO_BEFORE_MULTIPLAYER
global using NoteSpawnData = global::BeatmapObjectSpawnMovementData.NoteSpawnData;
#endif
#endif

namespace Legato.Gameplay {
    internal static class NativeMovement {
#if LEGATO_BEFORE_MULTIPLAYER
        internal static float JumpDistance(BeatmapObjectSpawnController controller) =>
            controller._beatmapObjectSpawnMovementData._jumpDistance;
#endif

        internal static float HalfJumpDuration(IVariableMovementDataProvider movement) =>
#if !LEGATO_VARIABLE_MOVEMENT_DATA
            movement.jumpDuration * 0.5f;
#else
            movement.halfJumpDuration;
#endif
    }
}
