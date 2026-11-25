#if LEGATO_HECK_LEGACY_GAMEPLAY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck.Animation;
using Heck.Deserialize;
using Heck.Settings;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace Heck.ReLoad;

public class ReLoader : ITickable
{
    private static readonly MethodInfo _despawnNote = AccessTools.Method(typeof(BeatmapObjectManager), "Despawn", new[] { typeof(NoteController) });
    private static readonly MethodInfo _despawnObstacle = AccessTools.Method(typeof(BeatmapObjectManager), "Despawn", new[] { typeof(ObstacleController) });
    private static readonly FieldInfo[] _notePools = new[] { "_noteAPool", "_noteBPool", "_bombNotePool" }
        .Select(name => AccessTools.Field(typeof(BeatmapObjectManager), name)).ToArray();
    private static readonly FieldInfo _obstaclePool = AccessTools.Field(typeof(BeatmapObjectManager), "_obstaclePool");
    private static readonly FieldInfo _soundPool = AccessTools.Field(typeof(NoteCutSoundEffectManager), "_noteCutSoundEffectPool");
    private static readonly FieldInfo _previousLeftSound = AccessTools.Field(typeof(NoteCutSoundEffectManager), "_prevNoteATime");
    private static readonly FieldInfo _previousRightSound = AccessTools.Field(typeof(NoteCutSoundEffectManager), "_prevNoteBTime");
    private static readonly FieldInfo _audioInitField = AccessTools.Field(typeof(AudioTimeSyncController), "_initData");
    private static readonly FieldInfo _startTime = AccessTools.Field(typeof(BeatmapObjectCallbackController), "_spawningStartTime");
    private static readonly FieldInfo _eventCallbacks = AccessTools.Field(typeof(BeatmapObjectCallbackController), "_beatmapEventCallbackData");
    private static readonly FieldInfo _rotationProcessor = AccessTools.Field(typeof(BeatmapObjectSpawnController), "_spawnRotationProcesser");
    private static readonly FieldInfo _tempoProcessor = AccessTools.Field(typeof(BeatmapObjectSpawnController), "_variableBPMProcessor");

    private readonly SiraLog _log;
    private readonly Config.ReLoaderSettings _config;
    private readonly ReLoaderLoader _loader;
    private readonly AudioTimeSyncController _audio;
    private readonly GameplayCoreSceneSetupData _setup;
    private readonly BeatmapData _chart;
    private readonly BeatmapObjectManager _objects;
    private readonly BeatmapObjectCallbackController _callbacks;
    private readonly BeatmapObjectSpawnController _spawner;
    private readonly NoteCutSoundEffectManager _sounds;
    private readonly DeserializerManager _deserializer;
    private readonly Dictionary<string, Track> _tracks;
    private readonly DiContainer _container;
    private float _savedTime;

    private ReLoader(
        SiraLog log,
        Config.ReLoaderSettings config,
        ReLoaderLoader loader,
        AudioTimeSyncController audio,
        AudioTimeSyncController.InitData initialTime,
        GameplayCoreSceneSetupData setup,
        BeatmapData chart,
        BeatmapObjectManager objects,
        BeatmapObjectCallbackController callbacks,
        BeatmapObjectSpawnController spawner,
        NoteCutSoundEffectManager sounds,
        DeserializerManager deserializer,
        Dictionary<string, Track> tracks,
        DiContainer container)
    {
        _log = log;
        _config = config;
        _loader = loader;
        _audio = audio;
        _setup = setup;
        _chart = chart;
        _objects = objects;
        _callbacks = callbacks;
        _spawner = spawner;
        _sounds = sounds;
        _deserializer = deserializer;
        _tracks = tracks;
        _container = container;
        _savedTime = initialTime.startSongTime;
    }

    public event Action? Reloaded;

    public event Action? Rewinded;

    public void Tick()
    {
        if (_audio.state == AudioTimeSyncController.State.Stopped)
        {
            return;
        }

        if (Input.GetKeyDown(_config.SaveTime))
        {
            _savedTime = _audio.songTime;
            return;
        }

        bool reload = Input.GetKeyDown(_config.Reload);
        if (Input.GetKeyDown(_config.JumpToSavedTime))
        {
            Seek(_savedTime, reload);
        }
        else if (reload)
        {
            Seek(_audio.songTime, true);
        }
        else if (Input.GetKeyDown(_config.ScrubBackwards))
        {
            Seek(_audio.songTime - _config.ScrubIncrement, false);
        }
        else if (Input.GetKeyDown(_config.ScrubForwards))
        {
            Seek(_audio.songTime + _config.ScrubIncrement, false);
        }
    }

    private void Seek(float time, bool reload)
    {
        IDifficultyBeatmap difficulty = _setup.difficultyBeatmap;
        CustomBeatmapData? original = null;
        CustomBeatmapData? transformed = null;
        HashSet<(object? Id, DeserializedData DeserializedData)>? data = null;
        Dictionary<string, Track>? tracks = null;
        if (reload && difficulty is CustomDifficultyBeatmap && _chart is CustomBeatmapData)
        {
            original = _loader.ReadChart(difficulty);
            if (original != null)
            {
                transformed = BeatDataTransformHelper.CreateTransformedBeatmapData(original, _setup.gameplayModifiers,
                    _setup.practiceSettings, _setup.playerSpecificSettings) as CustomBeatmapData;
                if (transformed == null)
                {
                    _log.Error("Chart custom data was lost during transformation");
                    return;
                }

                _deserializer.DeserializeBeatmapData(difficulty, transformed, original, _setup.playerSpecificSettings.leftHanded, out tracks, out data);
            }
        }

        ClearActiveObjects();
        foreach (Track track in _tracks.Values)
        {
            track.NullProperties();
        }

        if (transformed != null && original != null && _chart is CustomBeatmapData liveChart && difficulty is CustomDifficultyBeatmap customDifficulty)
        {
            _loader.ReplaceOriginal(customDifficulty, original);
            liveChart.ReplaceChartFrom(transformed);
            _tracks.Clear();
            foreach (KeyValuePair<string, Track> track in tracks!)
            {
                _tracks.Add(track.Key, track.Value);
            }

            foreach ((object? id, DeserializedData deserializedData) in data!)
            {
                _container.ResolveId<DeserializedData>(id).Remap(deserializedData);
            }

            Reloaded?.Invoke();
        }

        float songTime = Mathf.Clamp(time, 0, Mathf.Max(0, _audio.songEndTime - 0.01f));
        _rotationProcessor.SetValue(_spawner, new SpawnRotationProcessor());
        ((VariableBPMProcessor)_tempoProcessor.GetValue(_spawner)).SetBPM(difficulty.level.beatsPerMinute);
        _startTime.SetValue(_callbacks, songTime);
        _callbacks.SetNewBeatmapData(_chart);
        foreach (var callback in (List<BeatmapObjectCallbackController.BeatmapEventCallbackData>)_eventCallbacks.GetValue(_callbacks))
        {
            callback.nextEventIndex = 0;
        }

        SeekAudio(songTime);
        Rewinded?.Invoke();
    }

    private void ClearActiveObjects()
    {
        foreach (FieldInfo field in _notePools)
        {
            foreach (NoteController note in ((NoteController.Pool)field.GetValue(_objects)).activeItems.ToArray())
            {
                _despawnNote.Invoke(_objects, new object[] { note });
            }
        }

        foreach (ObstacleController obstacle in ((ObstacleController.Pool)_obstaclePool.GetValue(_objects)).activeItems.ToArray())
        {
            _despawnObstacle.Invoke(_objects, new object[] { obstacle });
        }

        foreach (NoteCutSoundEffect sound in ((NoteCutSoundEffect.Pool)_soundPool.GetValue(_sounds)).activeItems.ToArray())
        {
            sound.StopPlayingAndFinish();
        }

        _previousLeftSound.SetValue(_sounds, -1f);
        _previousRightSound.SetValue(_sounds, -1f);
    }

    private void SeekAudio(float time)
    {
        var previous = (AudioTimeSyncController.InitData)_audioInitField.GetValue(_audio);
        bool paused = _audio.state == AudioTimeSyncController.State.Paused;
        _audio.StopSong();
        _audioInitField.SetValue(_audio, new AudioTimeSyncController.InitData(previous.audioClip, time, previous.songTimeOffset, previous.timeScale));
        _audio.StartSong();
        if (paused)
        {
            _audio.Pause();
        }
    }
}
#endif
