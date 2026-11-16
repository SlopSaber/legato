namespace Legato.Presentation {
    internal static class PreviewNotes {
        internal static NoteData Normal() => NoteData.CreateBasicNoteData(
#if BEAT_SABER_1_29_0
            0, 0, 0, 0, 0);
#else
            0, 0, 0, 0, 0, 0, 0);
#endif
        internal static NoteData ChainHead() => new NoteData(
#if BEAT_SABER_1_29_0
            0, 0, 0, 0, NoteData.GameplayType.BurstSliderHead, NoteData.ScoringType.BurstSliderHead,
#else
            0, 0, 0, 0, 0, 0, NoteData.GameplayType.BurstSliderHead, NoteData.ScoringType.ChainHead,
#endif
            0, 0, 0, 0, 0, 0, 0, 0);
        internal static NoteData ChainLink() => new NoteData(
#if BEAT_SABER_1_29_0
            0, 0, 0, 0, NoteData.GameplayType.BurstSliderElement, NoteData.ScoringType.BurstSliderElement,
#else
            0, 0, 0, 0, 0, 0, NoteData.GameplayType.BurstSliderElement, NoteData.ScoringType.ChainLink,
#endif
            0, 0, 0, 0, 0, 0, 0, 0);
    }
}
