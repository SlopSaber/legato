#if !LEGATO_FIXED_NOTE_JUMP_SPEED || BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
#endif
using MonoMod.RuntimeDetour;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// Allows negative note jump speed, which is otherwise reset to the default NJS value.
    /// When a negative NJS is used, notes will come from behind the player and spin toward them.
    /// </summary>
    /// <example>
    /// https://beatsaver.com/maps/6cd
    /// </example>
    internal class AllowNegativeNoteJumpSpeedHook : IInitializable, IDisposable
    {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private ILHook _negativeNoteJumpSpeedHook = null!;
#else
        private Hook _negativeNoteJumpSpeedHook = null!;
#endif

        public void Initialize()
        {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            _negativeNoteJumpSpeedHook = new ILHook(typeof(GameplayCoreInstaller).GetMethod(nameof(GameplayCoreInstaller.InstallBindings))!, context =>
            {
                var cursor = new ILCursor(context);
                cursor.GotoNext(MoveType.After, instruction => instruction.MatchCallvirt<IDifficultyBeatmap>("get_noteJumpMovementSpeed"));
                cursor.GotoNext(MoveType.Before, instruction => instruction.MatchLdcR4(0), instruction => instruction.OpCode == OpCodes.Bgt_Un_S || instruction.OpCode == OpCodes.Bgt_Un);
                cursor.EmitDelegate<Func<float, float>>(Mathf.Abs);
            }, true);
#else
            _negativeNoteJumpSpeedHook = new Hook(typeof(BeatmapDifficultyMethods).GetMethod(nameof(BeatmapDifficultyMethods.NoteJumpMovementSpeed))!, AllowNegativeJumpSpeed, true);
#endif
        }

        public void Dispose()
        {
            _negativeNoteJumpSpeedHook.Dispose();
        }

#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
        private float AllowNegativeJumpSpeed(Func<BeatmapDifficulty, float, bool, float> original, BeatmapDifficulty difficulty, float noteJumpMovementSpeed, bool fastNotes)
        {
            return noteJumpMovementSpeed <= -VariableMovementDataProvider.kMinNoteJumpMovementSpeed ? noteJumpMovementSpeed : original(difficulty, noteJumpMovementSpeed, fastNotes);
        }
#endif
    }
}
#endif
