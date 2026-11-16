namespace Legato.Gameplay {
    // Serialized scoring identities remain stable; legacy native gameplay emits only -1 through 5.
    internal static class NativeScoring {
        internal const NoteData.ScoringType ArcHead = (NoteData.ScoringType)2;
        internal const NoteData.ScoringType ArcTail = (NoteData.ScoringType)3;
        internal const NoteData.ScoringType ChainHead = (NoteData.ScoringType)4;
        internal const NoteData.ScoringType ChainLink = (NoteData.ScoringType)5;
        internal const NoteData.ScoringType ArcHeadArcTail = (NoteData.ScoringType)6;
        internal const NoteData.ScoringType ChainHeadArcTail = (NoteData.ScoringType)7;
        internal const NoteData.ScoringType ChainLinkArcHead = (NoteData.ScoringType)8;
        internal const NoteData.ScoringType ChainHeadArcHead = (NoteData.ScoringType)9;
        internal const NoteData.ScoringType ChainHeadArcHeadArcTail = (NoteData.ScoringType)10;
    }
}
