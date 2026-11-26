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

        internal static float ReplayJumpOffset(BeatmapObjectSpawnMovementData movement, float distance, float speed, float bpm) {
            var travelPerBeat = speed * (60f / bpm);
            var baseBeats = movement._startHalfJumpDurationInBeats;
            while (baseBeats * travelPerBeat > movement._maxHalfJumpDistance) baseBeats *= 0.5f;
            return System.Math.Max(1f, distance / (2f * travelPerBeat)) - baseBeats;
        }
#endif

        internal static float HalfJumpDuration(IVariableMovementDataProvider movement) =>
#if !LEGATO_VARIABLE_MOVEMENT_DATA
            movement.jumpDuration * 0.5f;
#else
            movement.halfJumpDuration;
#endif
    }
}
