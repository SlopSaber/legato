#if LEGATO_EARLY_STANDARD_MAPS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using VRUI;
using Zenject;

namespace Heck.PlayView;

public class StartStandardLevelParameters
{
    public StartStandardLevelParameters(
        MenuSceneSetupData menu,
        VRUIViewController parent,
        IStandardLevelDifficultyBeatmap difficultyBeatmap,
        GameplayOptions gameplayOptions,
        GameplayMode gameMode)
    {
        Menu = menu;
        Parent = parent;
        DifficultyBeatmap = difficultyBeatmap;
        GameplayOptions = gameplayOptions;
        GameMode = gameMode;
    }

    public IStandardLevelDifficultyBeatmap DifficultyBeatmap { get; }

    public IStandardLevel PreviewBeatmapLevel => DifficultyBeatmap.level;

    public GameplayOptions GameplayOptions { get; }

    public GameplayMode GameMode { get; }

    internal MenuSceneSetupData Menu { get; }

    internal VRUIViewController Parent { get; }

    public virtual StartStandardLevelParameters Copy()
    {
        GameplayOptions options = new()
        {
            mirror = GameplayOptions.mirror,
            noEnergy = GameplayOptions.noEnergy,
            staticLights = GameplayOptions.staticLights,
            obstaclesOption = GameplayOptions.obstaclesOption
        };
        return new StartStandardLevelParameters(Menu, Parent, DifficultyBeatmap, options, GameMode);
    }
}

public sealed class PlayViewManager : IDisposable
{
    private readonly IPlayViewController[] _views;
    private readonly EarlyPlaySession _session;
    private readonly Queue<IPlayViewController> _pending = new();
    private StartStandardLevelParameters? _parameters;
    private VRUIViewController? _activeView;

    [UsedImplicitly]
    private PlayViewManager([Inject(Optional = true, Source = InjectSources.Local)] IEnumerable<IPlayViewController> views, EarlyPlaySession session)
    {
        _views = views.OrderBy(view => view.GetType().GetCustomAttribute<PlayViewControllerSettingsAttribute>()?.Priority ?? 0).ToArray();
        _session = session;
        foreach (IPlayViewController view in _views)
        {
            view.Finished += Advance;
            if (view is IParameterModifier modifier)
            {
                modifier.ParametersModified += ModifyParameters;
            }
        }
    }

    public void Dispose()
    {
        foreach (IPlayViewController view in _views)
        {
            view.Finished -= Advance;
            if (view is IParameterModifier modifier)
            {
                modifier.ParametersModified -= ModifyParameters;
            }
        }

        _pending.Clear();
        _parameters = null;
    }

    [PublicAPI]
    public void ForceStart(StartStandardLevelParameters startParameters) => Begin(startParameters, true);

    internal void Begin(StartStandardLevelParameters parameters, bool force = false)
    {
        if (_parameters != null)
        {
            return;
        }

        _parameters = parameters;
        foreach (IPlayViewController view in _views)
        {
            if (view.Init(_parameters) && !force && view is VRUIViewController)
            {
                _pending.Enqueue(view);
            }
        }

        Advance();
    }

    private static void Invoke(IPlayViewController view, string method) =>
        AccessTools.Method(view.GetType(), method)?.Invoke(view, null);

    private void Advance()
    {
        if (_parameters == null)
        {
            return;
        }

        VRUIViewController? previous = _activeView;
        _activeView = null;
        if (previous != null && previous.parentViewController != null)
        {
            previous.DismissModalViewController(PresentNext);
        }
        else
        {
            PresentNext();
        }
    }

    private void PresentNext()
    {
        if (_parameters == null)
        {
            return;
        }

        if (_pending.Count > 0)
        {
            IPlayViewController next = _pending.Dequeue();
            _activeView = (VRUIViewController)next;
            _parameters.Parent.PresentModalViewController(_activeView, () => Invoke(next, "OnShow"));
            return;
        }

        StartStandardLevelParameters parameters = _parameters;
        _parameters = null;
        _session.OnFinish(() =>
        {
            foreach (IPlayViewController view in _views)
            {
                Invoke(view, "OnFinish");
            }
        });
        try
        {
            foreach (IPlayViewController view in _views)
            {
                Invoke(view, "OnPlay");
            }

            parameters.Menu.StartLevel(parameters.DifficultyBeatmap, parameters.GameplayOptions, parameters.GameMode);
        }
        catch
        {
            _session.Finish();
            throw;
        }
    }

    private void ModifyParameters(StartStandardLevelParameters parameters) => _parameters = parameters;
}

internal sealed class EarlyPlaySession : IAffinity, IDisposable
{
    private readonly Stack<Action> _finish = new();

    public void Dispose() => Finish();

    internal void OnFinish(Action callback) => _finish.Push(callback);

    [AffinityPrefix]
    [AffinityPatch(typeof(MainGameSceneSetupData), nameof(MainGameSceneSetupData.Finish))]
    internal void Finish()
    {
        while (_finish.Count > 0)
        {
            try
            {
                _finish.Pop().Invoke();
            }
            catch (Exception exception)
            {
                Plugin.Log.Error(exception);
            }
        }
    }
}
#endif
