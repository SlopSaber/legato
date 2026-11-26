#if !LEGATO_BEFORE_MULTIPLAYER
namespace Legato.Presentation {
    internal static class PreviewNotes {
        internal static NoteData Normal() => NoteData.CreateBasicNoteData(
            0,
#if LEGATO_BEATMAP_OBJECT_BEAT
            0,
#endif
#if LEGATO_BEATMAP_OBJECT_ROTATION
            0,
#endif
            0, 0, 0, 0);
        internal static NoteData ChainHead() => Chain(NoteData.GameplayType.BurstSliderHead,
#if LEGATO_SLIDER_SCORING_TYPES
            NoteData.ScoringType.BurstSliderHead);
#else
            NoteData.ScoringType.ChainHead);
#endif
        internal static NoteData ChainLink() => Chain(NoteData.GameplayType.BurstSliderElement,
#if LEGATO_SLIDER_SCORING_TYPES
            NoteData.ScoringType.BurstSliderElement);
#else
            NoteData.ScoringType.ChainLink);
#endif
        private static NoteData Chain(NoteData.GameplayType gameplayType, NoteData.ScoringType scoringType) => new NoteData(
            0,
#if LEGATO_BEATMAP_OBJECT_BEAT
            0,
#endif
#if LEGATO_BEATMAP_OBJECT_ROTATION
            0,
#endif
            0, 0, 0, gameplayType, scoringType,
            0, 0, 0, 0, 0, 0, 0, 0);
    }
}
#endif
