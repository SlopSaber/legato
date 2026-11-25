namespace Legato.Gameplay {
    internal static class SpawnData {
        internal static NoteData Bomb(float time, int line, NoteLineLayer layer) => NoteData.CreateBombNoteData(
#if BEAT_SABER_1_29_0 || (BEAT_SABER_1_37_1 && !LEGATO_BEATMAP_OBJECT_BEAT)
            time, line, layer);
#elif LEGATO_SPAWN_ROTATION_EVENTS
            time, 0f, line, layer);
#else
            time, 0f, 0, line, layer);
#endif
        internal static ObstacleData Obstacle(float time, int line, NoteLineLayer layer, float duration, int width, int height) => new ObstacleData(
#if BEAT_SABER_1_29_0 || (BEAT_SABER_1_37_1 && !LEGATO_BEATMAP_OBJECT_BEAT)
            time, line, layer, duration, width, height);
#elif LEGATO_SPAWN_ROTATION_EVENTS
            time, 0f, 0f, line, layer, duration, width, height);
#else
            time, 0f, 0f, 0, line, layer, duration, width, height);
#endif
    }
}
