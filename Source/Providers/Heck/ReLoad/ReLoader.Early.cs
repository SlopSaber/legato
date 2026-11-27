#if LEGATO_EARLY_STANDARD_MAPS
using System;
using System.IO;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck.Settings;
using Legato.Beatmaps;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace Heck.ReLoad;

public class ReLoader : ITickable
{
    private readonly Config.ReLoaderSettings _config;
    private readonly ReLoaderLoader _loader;
    private readonly AudioTimeSyncController _audio;
    private readonly MainGameSceneSetupData _setup;
    private bool _transitioning;

    private ReLoader(Config.ReLoaderSettings config, ReLoaderLoader loader, AudioTimeSyncController audio, MainGameSceneSetupData setup)
    {
        _config = config;
        _loader = loader;
        _audio = audio;
        _setup = setup;
    }

    public event Action? Reloaded;

    public event Action? Rewinded;

    public void Tick()
    {
        if (_transitioning || !_audio.isActiveAndEnabled)
        {
            return;
        }

        IStandardLevelDifficultyBeatmap difficulty = _setup.difficultyLevel;
        if (Input.GetKeyDown(_config.SaveTime))
        {
            _loader.SaveTime(difficulty, _audio.songTime);
            return;
        }

        bool reload = Input.GetKeyDown(_config.Reload);
        if (Input.GetKeyDown(_config.JumpToSavedTime))
        {
            Seek(_loader.GetSavedTime(difficulty, _setup.forcedStartSongTime), reload);
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
        IStandardLevelDifficultyBeatmap difficulty = _setup.difficultyLevel;
        if (reload)
        {
            if (!_loader.Reload(difficulty))
            {
                return;
            }

            Reloaded?.Invoke();
        }

        Rewinded?.Invoke();
        GameplayOptions original = _setup.gameplayOptions;
        GameplayOptions practiceOptions = new()
        {
            mirror = original.mirror,
            staticLights = original.staticLights,
            obstaclesOption = original.obstaclesOption,
            noEnergy = true
        };
        float startTime = Mathf.Clamp(time, 0, Mathf.Max(0, _audio.songLength - 0.01f));
        _setup.Init(difficulty, practiceOptions, _setup.gameplayMode, startTime);
        _transitioning = true;
        _setup.TransitionToScene(0.35f);
    }
}

public class ReLoaderLoader
{
    private static readonly AccessTools.FieldRef<StandardLevelSO.DifficultyBeatmap, BeatmapDataSO> _asset =
        AccessTools.FieldRefAccess<StandardLevelSO.DifficultyBeatmap, BeatmapDataSO>("_beatmapData");

    private readonly SiraLog _log;
    private IStandardLevelDifficultyBeatmap? _savedDifficulty;
    private float _savedTime;

    private ReLoaderLoader(SiraLog log)
    {
        _log = log;
    }

    public bool Reload(IStandardLevelDifficultyBeatmap difficulty)
    {
        BeatmapDataSO? asset = difficulty is StandardLevelSO.DifficultyBeatmap native ? _asset(native) : null;
        string? file = asset != null ? EarlyBeatmapSources.GetFile(asset) : null;
        if (asset == null || file == null)
        {
            _log.Warn("The selected beatmap has no custom chart file to reload.");
            return false;
        }

        BeatmapDataSO staged = ScriptableObject.CreateInstance<BeatmapDataSO>();
        try
        {
            string json = File.ReadAllText(file);
            IStandardLevel level = difficulty.level;
            staged.SetRequiredDataForLoad(level.beatsPerMinute, level.shuffle, level.shufflePeriod);
            staged.SetJsonData(json);
            if (EarlyBeatmapSources.GetImportedMetadata(asset) is { } metadata)
            {
                EarlyBeatmapSources.AssociateMetadata(staged, metadata.Item1, metadata.Item2);
            }

            if (staged.beatmapData is not CustomBeatmapData chart)
            {
                _log.Warn("The updated chart could not be read as custom v2 data.");
                return false;
            }

            asset.SetJsonData(json);
            asset.beatmapData = chart;
            _log.Trace("Reloaded beatmap");
            return true;
        }
        catch (Exception exception)
        {
            _log.Error($"Could not reload the chart: {exception.Message}");
            return false;
        }
        finally
        {
            UnityEngine.Object.Destroy(staged);
        }
    }

    internal void SaveTime(IStandardLevelDifficultyBeatmap difficulty, float time)
    {
        _savedDifficulty = difficulty;
        _savedTime = time;
    }

    internal float GetSavedTime(IStandardLevelDifficultyBeatmap difficulty, float fallback) =>
        ReferenceEquals(difficulty, _savedDifficulty) ? _savedTime : fallback;
}

internal sealed class ReLoadRestart : IAffinity
{
    private readonly Config.ReLoaderSettings _config;
    private readonly ReLoaderLoader _loader;

    private ReLoadRestart(Config.ReLoaderSettings config, ReLoaderLoader loader)
    {
        _config = config;
        _loader = loader;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(RestartLevelController), nameof(RestartLevelController.RestartLevel))]
    private void ReloadOnRestart(MainGameSceneSetupData ____mainGameSceneSetupData)
    {
        if (_config.ReloadOnRestart)
        {
            _loader.Reload(____mainGameSceneSetupData.difficultyLevel);
        }
    }
}

public sealed class MenuReLoad : ITickable
{
    private readonly Config.ReLoaderSettings _config;
    private readonly ReLoaderLoader _loader;

    private MenuReLoad(Config.ReLoaderSettings config, ReLoaderLoader loader)
    {
        _config = config;
        _loader = loader;
    }

    public void Tick()
    {
        if (!Input.GetKeyDown(_config.Reload))
        {
            return;
        }

        StandardLevelDetailViewController detail = UnityEngine.Object.FindObjectsOfType<StandardLevelDetailViewController>()
            .FirstOrDefault(view => view.isInViewControllerHierarchy);
        if (detail != null && detail.difficultyLevel is { } difficulty)
        {
            _loader.Reload(difficulty);
        }
    }
}
#endif
