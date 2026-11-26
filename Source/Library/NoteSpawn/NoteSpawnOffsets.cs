#if !LEGATO_BEFORE_MULTIPLAYER
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_29_4 || BEAT_SABER_1_34_2 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
global using NativeNoteSpawnData = global::BeatmapObjectSpawnMovementData.NoteSpawnData;
#else
global using NativeNoteSpawnData = global::NoteSpawnData;
#endif
#endif

using UnityEngine;

namespace Legato.Gameplay
{
    internal static class NoteSpawnOffsets
    {
#if LEGATO_BEFORE_MULTIPLAYER
        internal static bool IsScoredNote(NoteData note) => note.noteType is NoteType.NoteA or NoteType.NoteB;

        internal static void Apply(ref Vector3 moveStart, ref Vector3 moveEnd, ref Vector3 jumpEnd, Vector3 offset)
        {
            moveStart += offset;
            moveEnd += offset;
            jumpEnd += offset;
        }
#else
        internal static NativeNoteSpawnData Apply(NativeNoteSpawnData source, Vector3 offset)
        {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_29_4 || BEAT_SABER_1_34_2 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
            return new NativeNoteSpawnData(source.moveStartPos + offset, source.moveEndPos + offset,
                source.jumpEndPos + offset, source.jumpGravity, source.moveDuration, source.jumpDuration);
#else
            return new NativeNoteSpawnData(source.moveStartOffset + offset, source.moveEndOffset + offset,
                source.jumpEndOffset + offset, source.gravityBase);
#endif
        }
#endif
    }
}
