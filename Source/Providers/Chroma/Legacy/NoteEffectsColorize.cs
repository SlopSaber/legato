#if LEGATO_CHROMA_LEGACY_LIGHTS
using Chroma.Colorizer;
using SiraUtil.Affinity;
using UnityEngine;

namespace Chroma.HarmonyPatches.Colorizer;

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
#endif
