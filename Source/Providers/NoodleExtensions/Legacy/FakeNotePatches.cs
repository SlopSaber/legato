#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using System.Collections.Generic;
using NoodleExtensions.Managers;
using SiraUtil.Affinity;
using Zenject;

namespace NoodleExtensions.HarmonyPatches.FakeNotes;

internal sealed class FakeNotePatches : IAffinity
{
    private readonly FakePatchesManager _fakePatchesManager;
    private readonly NoteCutEffectSpawner? _effects;
    private bool _drawingFakeCut;

    private FakeNotePatches(
        FakePatchesManager fakePatchesManager,
        [InjectOptional] NoteCutEffectSpawner? effects)
    {
        _fakePatchesManager = fakePatchesManager;
        _effects = effects;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(BeatmapObjectManager), "HandleNoteWasMissed")]
    private bool ReportMiss(NoteController noteController) => _fakePatchesManager.IsRealNote(noteController.noteData);

    [AffinityPrefix]
    [AffinityPatch(typeof(BeatmapObjectManager), "HandleNoteWasCut")]
    private bool ReportCut(BeatmapObjectManager __instance, NoteController noteController, NoteCutInfo noteCutInfo)
    {
        if (_fakePatchesManager.IsRealNote(noteController.noteData))
        {
            return true;
        }

        _drawingFakeCut = true;
        try
        {
            if (_effects != null)
            {
                if (noteController.noteData.noteType == NoteType.Bomb)
                {
                    _effects.SpawnBombCutEffect(noteCutInfo.cutPoint, noteController, noteCutInfo);
                }
                else
                {
                    _effects.SpawnNoteCutEffect(noteCutInfo.cutPoint, noteController, noteCutInfo);
                }
            }
        }
        finally
        {
            _drawingFakeCut = false;
            __instance.Despawn(noteController);
        }

        return false;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(FlyingScoreSpawner), nameof(FlyingScoreSpawner.SpawnFlyingScore))]
    [AffinityPatch(typeof(FlyingSpriteSpawner), nameof(FlyingSpriteSpawner.SpawnFlyingSprite))]
    private bool ShowScore() => !_drawingFakeCut;

    [AffinityPrefix]
    [AffinityPatch(typeof(NoteCutSoundEffectManager), "BeatmapObjectCallback")]
    private bool ScheduleCutSound(BeatmapObjectData beatmapObjectData) =>
        beatmapObjectData is not NoteData note || _fakePatchesManager.IsRealNote(note);

    [AffinityPostfix]
    [AffinityPatch(typeof(BombNoteController), nameof(BombNoteController.Init))]
    private void ApplyBombInteractability(NoteData noteData, CuttableBySaber ____cuttableBySaber)
    {
        if (!_fakePatchesManager.GetCuttable(noteData))
        {
            ____cuttableBySaber.canBeCut = false;
        }
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(GameNoteController), "NoteDidStartJump")]
    private bool ApplyNoteInteractability(GameNoteController __instance) => _fakePatchesManager.GetCuttable(__instance.noteData);

    [AffinityPostfix]
    [AffinityPatch(typeof(PlayerHeadAndObstacleInteraction), nameof(PlayerHeadAndObstacleInteraction.GetObstaclesCointainingPoint))]
    private void FilterHeadCollisions(List<ObstacleController> obstacleControllers)
    {
        obstacleControllers.RemoveAll(_fakePatchesManager.BoundsNullCheck);
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(BeatmapObjectManager), "HandleObstaclePassedAvoidedMark")]
    private bool ReportAvoidedObstacle(ObstacleController obstacleController)
    {
        return !_fakePatchesManager.IsFakeObstacle(obstacleController.obstacleData);
    }
}
#endif
