#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using Heck.ReLoad;
using Vivify.ObjectPrefab.Collections;
using Vivify.ObjectPrefab.Pools;
using Xft;
using Zenject;

namespace Vivify.ObjectPrefab.Managers;

internal sealed class SaberPrefabManager : ILateTickable, IDisposable
{
    private readonly BeatmapObjectPrefabManager _prefabs;
    private readonly PlayerController _player;
    private readonly ReLoader? _reloader;
    private readonly Dictionary<IPrefabCollection, Func<bool>> _pending = new();

    private SaberPrefabManager(
        BeatmapObjectPrefabManager prefabs,
        PlayerController player,
        [InjectOptional] ReLoader? reloader)
    {
        _prefabs = prefabs;
        _player = player;
        _reloader = reloader;
        if (reloader != null)
        {
            reloader.Rewinded += Reset;
        }

        SaberAPrefabs.Changed += AssignModelA;
        SaberBPrefabs.Changed += AssignModelB;
        SaberATrailMaterials.Changed += AssignTrailA;
        SaberBTrailMaterials.Changed += AssignTrailB;
    }

    internal PrefabList SaberAPrefabs { get; } = new();

    internal TrailList SaberATrailMaterials { get; } = new();

    internal PrefabList SaberBPrefabs { get; } = new();

    internal TrailList SaberBTrailMaterials { get; } = new();

    public void Dispose()
    {
        if (_reloader != null)
        {
            _reloader.Rewinded -= Reset;
        }

        SaberAPrefabs.Changed -= AssignModelA;
        SaberBPrefabs.Changed -= AssignModelB;
        SaberATrailMaterials.Changed -= AssignTrailA;
        SaberBTrailMaterials.Changed -= AssignTrailB;
        Reset();
    }

    public void LateTick()
    {
        if (_pending.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<IPrefabCollection, Func<bool>> assignment in new List<KeyValuePair<IPrefabCollection, Func<bool>>>(_pending))
        {
            if (assignment.Value())
            {
                _pending.Remove(assignment.Key);
            }
        }
    }

    private void Reset()
    {
        _pending.Clear();
        SaberAPrefabs.Clear();
        SaberBPrefabs.Clear();
        SaberATrailMaterials.Clear();
        SaberBTrailMaterials.Clear();
    }

    private void AssignModelA(float time) => AssignModel(SaberType.SaberA, SaberAPrefabs, time);

    private void AssignModelB(float time) => AssignModel(SaberType.SaberB, SaberBPrefabs, time);

    private void AssignTrailA(float time) => AssignTrail(SaberType.SaberA, SaberATrailMaterials, time);

    private void AssignTrailB(float time) => AssignTrail(SaberType.SaberB, SaberBTrailMaterials, time);

    private void AssignModel(SaberType type, PrefabList assignment, float time)
    {
        ApplyOrDefer(assignment, () =>
        {
            SaberModelContainer? container = _player.SaberForType(type)?.GetComponentInChildren<SaberModelContainer>(true);
            if (container == null || container._saberModelController == null)
            {
                return false;
            }

            _prefabs.Despawn(container);
            _prefabs.Spawn<PrefabPool, UnityEngine.GameObject>(assignment, container, time);
            return true;
        });
    }

    private void AssignTrail(SaberType type, TrailList assignment, float time)
    {
        ApplyOrDefer(assignment, () =>
        {
            SaberModelContainer? container = _player.SaberForType(type)?.GetComponentInChildren<SaberModelContainer>(true);
            XWeaponTrail? trail = (container?._saberModelController as BasicSaberModelController)?._saberWeaponTrail;
            trail ??= container?.GetComponentInChildren<XWeaponTrail>(true);
            if (trail == null || trail._trailRenderer == null)
            {
                return false;
            }

            _prefabs.Despawn(trail);
            _prefabs.Spawn<TrailPool, FollowedSaberTrail>(assignment, trail, time);
            return true;
        });
    }

    private void ApplyOrDefer(IPrefabCollection assignment, Func<bool> apply)
    {
        _pending.Remove(assignment);
        if (!apply())
        {
            _pending.Add(assignment, apply);
        }
    }
}
#endif
