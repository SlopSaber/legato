#if LEGATO_FIXED_NOTE_JUMP_SPEED && !BEAT_SABER_1_29_0
using System;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Zenject;
using NoteJumpValueType = BeatmapObjectSpawnMovementData.NoteJumpValueType;

namespace SongCore.Hooks
{
    internal sealed class AllowNegativeNoteJumpSpeedHook : IInitializable, IDisposable
    {
        private readonly BeatmapBasicData _beatmapBasicData;
        private Hook? _hook;

        private AllowNegativeNoteJumpSpeedHook(BeatmapBasicData beatmapBasicData) => _beatmapBasicData = beatmapBasicData;

        public void Initialize() => _hook = new Hook(typeof(BeatmapObjectSpawnMovementData).GetMethod(nameof(BeatmapObjectSpawnMovementData.Init))!, InitializeMovement, true);

        public void Dispose() => _hook?.Dispose();

        private void InitializeMovement(
            Action<BeatmapObjectSpawnMovementData, int, float, float, NoteJumpValueType, float, IJumpOffsetYProvider, Vector3, Vector3> original,
            BeatmapObjectSpawnMovementData movement, int noteLinesCount, float startSpeed, float bpm, NoteJumpValueType jumpValueType,
            float jumpValue, IJumpOffsetYProvider jumpOffsetYProvider, Vector3 right, Vector3 forward)
        {
            float speed = _beatmapBasicData.noteJumpMovementSpeed;
            original(movement, noteLinesCount, speed < 0 ? speed : startSpeed, bpm, jumpValueType, jumpValue, jumpOffsetYProvider, right, forward);
        }
    }
}
#endif
