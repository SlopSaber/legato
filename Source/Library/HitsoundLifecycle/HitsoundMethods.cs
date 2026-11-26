namespace Legato.Audio {
    internal static class HitsoundMethods {
#if LEGATO_BEFORE_MULTIPLAYER
        internal const string EffectLateUpdate = "LateUpdate";
        internal const string SpawnEffect = "BeatmapObjectCallback";
#elif BEAT_SABER_1_29_0
        internal const string EffectLateUpdate = "LateUpdate";
        internal const string SpawnEffect = "HandleNoteWasSpawned";
#else
        internal const string EffectLateUpdate = "OnLateUpdate";
        internal const string SpawnEffect = "SpawnNoteCutSoundEffect";
#endif
    }
}
