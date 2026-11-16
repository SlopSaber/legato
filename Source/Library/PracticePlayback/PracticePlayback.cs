namespace Legato.Gameplay {
    internal static class PracticePlayback {
        internal static float SpawnAheadTime(BeatmapObjectSpawnController controller) =>
#if BEAT_SABER_1_29_0
            controller._beatmapObjectSpawnMovementData.spawnAheadTime;
#else
            controller._variableMovementDataProvider.spawnAheadTime;
#endif
    }
}
