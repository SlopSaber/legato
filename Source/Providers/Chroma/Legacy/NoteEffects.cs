#if LEGATO_CHROMA_LEGACY_LIGHTS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Chroma.Colorizer;
using HarmonyLib;
using Heck;
using Heck.Deserialize;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;

namespace Chroma.HarmonyPatches
{
    internal sealed class BeatEffectSpawnerSkip : IAffinity
    {
        private readonly DeserializedData _data;

        private BeatEffectSpawnerSkip([Inject(Id = ChromaController.ID)] DeserializedData data)
        {
            _data = data;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(BeatEffectSpawner), "HandleNoteDidStartJumpEvent")]
        private bool ShouldSpawn(NoteController noteController)
        {
            return !_data.Resolve(noteController.noteData, out ChromaNoteData? custom) || custom.SpawnEffect != false;
        }
    }

    internal sealed class DebrisSpawnerSkip : IAffinity, IDisposable
    {
        private static readonly MethodInfo _spawnExplosion = AccessTools.Method(typeof(BombExplosionEffect), nameof(BombExplosionEffect.SpawnExplosion));
        private readonly DeserializedData _data;
        private readonly CodeInstruction _checkBombDebris;

        private DebrisSpawnerSkip([Inject(Id = ChromaController.ID)] DeserializedData data)
        {
            _data = data;
            _checkBombDebris = InstanceTranspilers.EmitInstanceDelegate<Action<BombExplosionEffect, Vector3, INoteController>>(SpawnBombExplosion);
        }

        public void Dispose() => InstanceTranspilers.DisposeDelegate(_checkBombDebris);

        [AffinityPrefix]
        [AffinityPatch(typeof(NoteDebrisSpawner), nameof(NoteDebrisSpawner.SpawnDebris))]
        private bool ShouldSpawnDebris(INoteController noteController)
        {
            return !_data.Resolve(noteController.noteData, out ChromaNoteData? custom) || custom.DisableDebris != true;
        }

        [AffinityTranspiler]
        [AffinityPatch(typeof(NoteCutEffectSpawner), "SpawnBombCutEffect")]
        private IEnumerable<CodeInstruction> CheckBombDebris(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && Equals(instruction.operand, _spawnExplosion))
                {
                    var loadNote = new CodeInstruction(OpCodes.Ldarg_2);
                    loadNote.labels.AddRange(instruction.labels);
                    loadNote.blocks.AddRange(instruction.blocks);
                    yield return loadNote;
                    yield return new CodeInstruction(_checkBombDebris.opcode, _checkBombDebris.operand);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private void SpawnBombExplosion(BombExplosionEffect effect, Vector3 position, INoteController note)
        {
            if (ShouldSpawnDebris(note))
            {
                effect.SpawnExplosion(position);
            }
        }
    }
}

namespace Chroma.HarmonyPatches.Colorizer
{
    internal sealed class NoteEffectsColorize : IAffinity
    {
        private readonly NoteColorizerManager _notes;
        private readonly BombColorizerManager _bombs;
        private Color? _color;

        private NoteEffectsColorize(NoteColorizerManager notes, BombColorizerManager bombs)
        {
            _notes = notes;
            _bombs = bombs;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(BeatEffectSpawner), "HandleNoteDidStartJumpEvent")]
        private void SelectSpawnColor(NoteController noteController) => _color = GetColor(noteController);

        [AffinityPrefix]
        [AffinityPatch(typeof(NoteCutEffectSpawner), "SpawnNoteCutEffect")]
        private void SelectCutColor(INoteController noteController) => _color = GetColor(noteController as NoteController);

        [AffinityPostfix]
        [AffinityPatch(typeof(BeatEffectSpawner), "HandleNoteDidStartJumpEvent")]
        [AffinityPatch(typeof(NoteCutEffectSpawner), "SpawnNoteCutEffect")]
        private void ClearColor() => _color = null;

        [AffinityPrefix]
        [AffinityPatch(typeof(ColorManager), nameof(ColorManager.ColorForNoteType))]
        private bool UseColor(ref Color __result)
        {
            if (_color is not Color color)
            {
                return true;
            }

            __result = color;
            return false;
        }

        private Color? GetColor(NoteController? controller)
        {
            if (controller == null)
            {
                return null;
            }

            if (_notes.Colorizers.TryGetValue(controller, out NoteColorizer note))
            {
                return note.Color;
            }

            return _bombs.Colorizers.TryGetValue(controller, out BombColorizer bomb) ? bomb.Color : null;
        }
    }
}
#endif
