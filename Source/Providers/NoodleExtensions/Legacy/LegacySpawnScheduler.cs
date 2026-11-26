#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using System.Linq;
using Heck.Deserialize;
using Heck.ReLoad;
using SiraUtil.Affinity;
using Zenject;

namespace NoodleExtensions.HarmonyPatches.ObjectProcessing;

internal sealed class LegacySpawnScheduler : IAffinity, IDisposable
{
    private readonly DeserializedData _data;
    private readonly ReLoader? _reloader;
    private BeatmapObjectSpawnController? _spawner;
    private BeatmapData? _chart;
    private ScheduledObject[] _schedule = [];
    private int _next;

    private LegacySpawnScheduler(
        [Inject(Id = NoodleController.ID)] DeserializedData data,
        [InjectOptional] ReLoader? reloader)
    {
        _data = data;
        _reloader = reloader;
        if (reloader != null)
        {
            reloader.Rewinded += InvalidateSchedule;
        }
    }

    public void Dispose()
    {
        if (_reloader != null)
        {
            _reloader.Rewinded -= InvalidateSchedule;
        }
    }

    private static Dictionary<NoteData, float> GetCutAngles(
        IReadOnlyList<BeatmapObjectData> items,
        BeatmapObjectSpawnMovementData movement)
    {
        Dictionary<NoteData, float> angles = new();
        BeatmapCallbackItemDataList rows = new(
            (note, angle) => angles[note] = angle,
            _ => { },
            _ => { },
            _ => { },
            () => { },
            movement.Get2DNoteOffset);

        foreach (BeatmapObjectData item in items)
        {
            if (item is NoteData)
            {
                rows.InsertBeatmapObjectData(item);
            }
        }

        rows.ProcessData();
        return angles;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(BeatmapObjectSpawnController), "Start")]
    private void TakeOverObjectCallbacks(BeatmapObjectSpawnController __instance)
    {
        _spawner = __instance;
        InvalidateSchedule();
        __instance._beatmapObjectCallbackController.RemoveBeatmapObjectCallback(__instance._beatmapObjectCallbackData);
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(BeatmapObjectCallbackController), "LateUpdate")]
    private void SpawnReadyObjects(BeatmapObjectCallbackController __instance)
    {
        if (_spawner == null || _spawner._beatmapObjectCallbackController != __instance ||
            _spawner._disableSpawning || __instance._beatmapData is not BeatmapData chart)
        {
            return;
        }

        if (!ReferenceEquals(_chart, chart))
        {
            BuildSchedule(_spawner, chart, __instance._spawningStartTime);
        }

        float songTime = __instance._audioTimeSource.songTime;
        if (_next >= _schedule.Length || _schedule[_next].SpawnTime >= songTime)
        {
            return;
        }

        BeatmapObjectSpawnMovementData movement = _spawner._beatmapObjectSpawnMovementData;
        VariableBPMProcessor tempo = _spawner._variableBPMProcessor;
        SpawnRotationProcessor rotation = _spawner._spawnRotationProcesser;
        float previousBpm = tempo.currentBPM;
        float previousRotation = rotation.rotation;
        try
        {
            while (_next < _schedule.Length && _schedule[_next].SpawnTime < songTime)
            {
                ScheduledObject entry = _schedule[_next++];
                if (entry.EndTime <= songTime)
                {
                    continue;
                }

                if (tempo.currentBPM != entry.Bpm)
                {
                    tempo.SetBPM(entry.Bpm);
                    movement.Update(entry.Bpm, _spawner._jumpOffsetY);
                }

                rotation._rotation = entry.Rotation;
                switch (entry.Item)
                {
                    case NoteData note:
                        _spawner.SpawnNote(note, entry.CutAngle);
                        break;
                    case ObstacleData obstacle:
                        _spawner.SpawnObstacle(obstacle);
                        break;
                }
            }
        }
        finally
        {
            rotation._rotation = previousRotation;
            if (tempo.currentBPM != previousBpm)
            {
                tempo.SetBPM(previousBpm);
                movement.Update(previousBpm, _spawner._jumpOffsetY);
            }
        }
    }

    private void InvalidateSchedule()
    {
        _chart = null;
        _schedule = [];
        _next = 0;
    }

    private void BuildSchedule(BeatmapObjectSpawnController spawner, BeatmapData chart, float startTime)
    {
        BeatmapObjectSpawnMovementData movement = spawner._beatmapObjectSpawnMovementData;
        BeatmapObjectData[] items = chart.beatmapLinesData
            .SelectMany(line => line.beatmapObjectsData)
            .Where(item => item.time >= startTime && item is NoteData or ObstacleData)
            .OrderBy(item => item.time)
            .ToArray();
        Dictionary<NoteData, float> cutAngles = GetCutAngles(items, movement);
        BeatmapEventData[] events = chart.beatmapEventData
            .Where(item => item.type.IsBPMChangeEvent() || item.type.IsRotationEvent())
            .OrderBy(item => item.time)
            .ThenByDescending(item => item.type.IsEarlyEvent())
            .ToArray();
        var rotation = new SpawnRotationProcessor();
        float bpm = movement._startBPM;
        int cursor = 0;
        var schedule = new List<ScheduledObject>(items.Length);
        foreach (BeatmapObjectData item in items)
        {
            while (cursor < events.Length &&
                   (events[cursor].time < item.time || (events[cursor].time == item.time && events[cursor].type.IsEarlyEvent())))
            {
                BeatmapEventData ev = events[cursor++];
                if (ev.type.IsBPMChangeEvent() && ev.value > 0)
                {
                    bpm = ev.value;
                }

                rotation.ProcessBeatmapEventData(ev);
            }

            _data.Resolve(item, out NoodleObjectData? data);
            float aheadTime = movement.moveDuration + (LegacySpawnMovement.JumpDuration(movement, data, bpm) * 0.5f);
            if (data != null)
            {
                data.InternalAheadTime = aheadTime;
            }

            float endTime = item.time + (item is ObstacleData obstacle ? obstacle.duration : 0f);
            float angle = item is NoteData note && cutAngles.TryGetValue(note, out float foundAngle) ? foundAngle : 0f;
            schedule.Add(new ScheduledObject(item, item.time - aheadTime, endTime, bpm, rotation.rotation, angle));
        }

        _schedule = schedule.OrderBy(entry => entry.SpawnTime).ToArray();
        _next = 0;
        _chart = chart;
    }

    private readonly struct ScheduledObject
    {
        internal ScheduledObject(BeatmapObjectData item, float spawnTime, float endTime, float bpm, float rotation, float cutAngle)
        {
            Item = item;
            SpawnTime = spawnTime;
            EndTime = endTime;
            Bpm = bpm;
            Rotation = rotation;
            CutAngle = cutAngle;
        }

        internal BeatmapObjectData Item { get; }

        internal float SpawnTime { get; }

        internal float EndTime { get; }

        internal float Bpm { get; }

        internal float Rotation { get; }

        internal float CutAngle { get; }
    }
}
#endif
