#if BEAT_SABER_1_29_0 || BEAT_SABER_1_29_4 || BEAT_SABER_1_34_2 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
global using NativeNoteSpawnData = global::BeatmapObjectSpawnMovementData.NoteSpawnData;
#else
global using NativeNoteSpawnData = global::NoteSpawnData;
#endif

using UnityEngine;

namespace Legato.Gameplay
{
    internal static class NoteSpawnOffsets
    {
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
    }
}
