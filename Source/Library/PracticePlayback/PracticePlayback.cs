namespace Legato.Gameplay {
    internal static class PracticePlayback {
        internal static float SpawnAheadTime(BeatmapObjectSpawnController controller) =>
#if !LEGATO_VARIABLE_MOVEMENT_DATA
            controller._beatmapObjectSpawnMovementData.spawnAheadTime;
#else
            controller._variableMovementDataProvider.spawnAheadTime;
#endif
    }
}
