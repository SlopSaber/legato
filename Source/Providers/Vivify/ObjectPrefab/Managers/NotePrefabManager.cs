using System;
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System.Collections.Generic;
using SiraUtil.Affinity;
#endif
using System.Linq;
using Heck.Animation;
using Heck.Deserialize;
using Heck.ReLoad;
using JetBrains.Annotations;
using Vivify.ObjectPrefab.Collections;
using Zenject;

namespace Vivify.ObjectPrefab.Managers;

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
internal class NotePrefabManager : IAffinity, IDisposable
#else
internal class NotePrefabManager : IDisposable
#endif
{
#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
    private readonly BasicBeatmapObjectManager? _basicBeatmapObjectManager;
#endif
    private readonly BeatmapObjectManager _beatmapObjectManager;
    private readonly DeserializedData _deserializedData;
    private readonly BeatmapObjectPrefabManager _prefabManager;

    private readonly ReLoader? _reLoader;

    [UsedImplicitly]
    private NotePrefabManager(
        BeatmapObjectPrefabManager prefabManager,
        BeatmapObjectManager beatmapObjectManager,
        [Inject(Id = VivifyController.ID)] DeserializedData deserializedData,
        [InjectOptional] ReLoader? reLoader)
    {
        _prefabManager = prefabManager;
        _beatmapObjectManager = beatmapObjectManager;
#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        _basicBeatmapObjectManager = beatmapObjectManager as BasicBeatmapObjectManager;
#endif
        _deserializedData = deserializedData;
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += OnRewind;
        }

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        AnyDirectionNotePrefabs.Changed += OnAnyDirectionNotePrefabsChanges;
        BombNotePrefabs.Changed += OnBombNotePrefabsChanged;
        ColorNotePrefabs.Changed += OnColorNotePrefabsChanges;
#else
        if (_basicBeatmapObjectManager != null)
        {
            AnyDirectionNotePrefabs.Changed += OnAnyDirectionNotePrefabsChanges;
            BombNotePrefabs.Changed += OnBombNotePrefabsChanged;
            BurstSliderElementPrefabs.Changed += OnBurstSliderElementPrefabsChanged;
            BurstSliderPrefabs.Changed += OnBurstSliderPrefabsChanged;
            ColorNotePrefabs.Changed += OnColorNotePrefabsChanges;
        }
#endif

        beatmapObjectManager.noteWasSpawnedEvent += HandleNoteWasSpawned;
#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        beatmapObjectManager.noteWasDespawnedEvent += HandleNoteWasDespawned;
#endif
    }

    internal PrefabDictionary AnyDirectionNotePrefabs { get; } = new();

    internal PrefabDictionary BombNotePrefabs { get; } = new();

    internal PrefabDictionary BurstSliderElementPrefabs { get; } = new();

    internal PrefabDictionary BurstSliderPrefabs { get; } = new();

    internal PrefabDictionary ColorNotePrefabs { get; } = new();

    public void Dispose()
    {
        if (_reLoader != null)
        {
            _reLoader.Rewinded -= OnRewind;
        }

        AnyDirectionNotePrefabs.Changed -= OnAnyDirectionNotePrefabsChanges;
        BombNotePrefabs.Changed -= OnBombNotePrefabsChanged;
#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        BurstSliderElementPrefabs.Changed -= OnBurstSliderElementPrefabsChanged;
        BurstSliderPrefabs.Changed -= OnBurstSliderPrefabsChanged;
#endif
        ColorNotePrefabs.Changed -= OnColorNotePrefabsChanges;

        _beatmapObjectManager.noteWasSpawnedEvent -= HandleNoteWasSpawned;
#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        _beatmapObjectManager.noteWasDespawnedEvent -= HandleNoteWasDespawned;
#endif
    }

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
    [AffinityPrefix]
    [AffinityPatch(typeof(BeatmapObjectManager), "Despawn", AffinityMethodType.Normal, new[] { typeof(NoteController) })]
#endif
    private void HandleNoteWasDespawned(NoteController noteController)
    {
        _prefabManager.Despawn(noteController);
    }

    private void HandleNoteWasSpawned(NoteController noteController)
    {
        NoteData noteData = noteController.noteData;

        if (!_deserializedData.Resolve(noteData, out VivifyObjectData? data) ||
            data.Track == null)
        {
            return;
        }

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        PrefabDictionary? prefabDictionary = noteData.noteType switch
        {
            NoteType.NoteA or NoteType.NoteB => noteData.cutDirection == NoteCutDirection.Any
                ? AnyDirectionNotePrefabs
                : ColorNotePrefabs,
            NoteType.Bomb => BombNotePrefabs,
            _ => null
        };
#else
        PrefabDictionary? prefabDictionary =
            noteData.gameplayType switch
            {
                NoteData.GameplayType.Normal => noteData.cutDirection == NoteCutDirection.Any
                    ? AnyDirectionNotePrefabs
                    : ColorNotePrefabs,
                NoteData.GameplayType.Bomb => BombNotePrefabs,
                NoteData.GameplayType.BurstSliderHead => BurstSliderPrefabs,
                NoteData.GameplayType.BurstSliderElement => BurstSliderElementPrefabs,
                _ => null
            };
#endif

        if (prefabDictionary == null)
        {
            return;
        }

        _prefabManager.Spawn(
            data.Track,
            prefabDictionary,
            noteController,
#if !PRE_V1_40_8
            noteController._noteMovement._floorMovement._beatTime);
#else
            noteController._noteMovement._floorMovement.startTime);
#endif
    }

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
    private void OnAnyDirectionNotePrefabsChanges(Track track)
    {
        RefreshNotes(track, AnyDirectionNotePrefabs, true);
    }

    private void OnColorNotePrefabsChanges(Track track)
    {
        RefreshNotes(track, ColorNotePrefabs, false);
    }

    private void OnBombNotePrefabsChanged(Track track)
    {
        foreach (NoteController controller in _beatmapObjectManager._bombNotePool.activeItems)
        {
            RefreshObjects(track, controller, BombNotePrefabs);
        }
    }

    private void RefreshNotes(Track track, PrefabDictionary prefabs, bool anyDirection)
    {
        IEnumerable<NoteController> notes =
            _beatmapObjectManager._noteAPool.activeItems.Concat(_beatmapObjectManager._noteBPool.activeItems);
        foreach (NoteController controller in notes)
        {
            if ((controller.noteData.cutDirection == NoteCutDirection.Any) == anyDirection)
            {
                RefreshObjects(track, controller, prefabs);
            }
        }
    }
#else
    private void OnAnyDirectionNotePrefabsChanges(Track track)
    {
        foreach (GameNoteController? noteController in _basicBeatmapObjectManager!._basicGameNotePoolContainer
                     .activeItems)
        {
            if (noteController.noteData.cutDirection == NoteCutDirection.Any)
            {
                RefreshObjects(track, noteController, AnyDirectionNotePrefabs);
            }
        }
    }

    private void OnBombNotePrefabsChanged(Track track)
    {
        foreach (BombNoteController noteController in
                 _basicBeatmapObjectManager!._bombNotePoolContainer.activeItems)
        {
            RefreshObjects(track, noteController, BombNotePrefabs);
        }
    }

    private void OnBurstSliderElementPrefabsChanged(Track track)
    {
        foreach (BurstSliderGameNoteController? noteController in _basicBeatmapObjectManager!
                     ._burstSliderGameNotePoolContainer.activeItems)
        {
            RefreshObjects(track, noteController, BurstSliderElementPrefabs);
        }
    }

    private void OnBurstSliderPrefabsChanged(Track track)
    {
        foreach (GameNoteController? noteController in _basicBeatmapObjectManager!
                     ._burstSliderHeadGameNotePoolContainer.activeItems)
        {
            RefreshObjects(track, noteController, BurstSliderPrefabs);
        }
    }

    private void OnColorNotePrefabsChanges(Track track)
    {
        foreach (GameNoteController? noteController in _basicBeatmapObjectManager!._basicGameNotePoolContainer
                     .activeItems)
        {
            if (noteController.noteData.cutDirection != NoteCutDirection.Any)
            {
                RefreshObjects(track, noteController, ColorNotePrefabs);
            }
        }
    }

#endif

    private void OnRewind()
    {
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        AnyDirectionNotePrefabs.Clear();
#endif
        ColorNotePrefabs.Clear();
        BombNotePrefabs.Clear();
        BurstSliderPrefabs.Clear();
        BurstSliderElementPrefabs.Clear();
    }

    private void RefreshObjects(Track track, NoteController noteController, PrefabDictionary prefabDictionary)
    {
        if (!_deserializedData.Resolve(noteController.noteData, out VivifyObjectData? data) ||
            data.Track == null ||
            !data.Track.Contains(track))
        {
            return;
        }

        _prefabManager.Despawn(noteController);
        _prefabManager.Spawn(
            data.Track,
            prefabDictionary,
            noteController,
#if !PRE_V1_40_8
            noteController._noteMovement._floorMovement._beatTime);
#else
            noteController._noteMovement._floorMovement.startTime);
#endif
    }
}
