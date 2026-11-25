#if LEGATO_CHROMA_LEGACY_LIGHTS
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Chroma.Lighting;

public abstract class LegacyColorTween
{
    private float _progress;

    public Color fromValue { get; set; }

    public Color toValue { get; set; }

    public float startTime { get; private set; }

    public float duration { get; set; }

    public bool isKilled { get; private set; } = true;

    public abstract Color GetValue(float time);

    public void ForceOnUpdate() => ApplyColor(GetValue(_progress));

    public void Kill()
    {
        isKilled = true;
        _progress = 0;
    }

    public void SetStartTimeAndEndTime(float start, float end)
    {
        startTime = start;
        duration = end - start;
    }

    internal void Resume() => isKilled = false;

    internal void UpdateAt(float time)
    {
        _progress = duration > 0 ? Mathf.Clamp01((time - startTime) / duration) : 1f;
        ForceOnUpdate();
        if (_progress >= 1f)
        {
            Kill();
        }
    }

    protected abstract void ApplyColor(Color color);
}

internal sealed class LegacySongTimeTweening : ITickable
{
    private readonly IAudioTimeSource _audio;
    private readonly Dictionary<ChromaIDColorTween, LightSwitchEventEffect> _active = new();
    private readonly List<ChromaIDColorTween> _retired = new();

    private LegacySongTimeTweening(IAudioTimeSource audio)
    {
        _audio = audio;
    }

    public void Tick()
    {
        float time = _audio.songTime;
        foreach (KeyValuePair<ChromaIDColorTween, LightSwitchEventEffect> item in _active)
        {
            ChromaIDColorTween tween = item.Key;
            if (!tween.isKilled && item.Value)
            {
                tween.UpdateAt(time);
            }

            if (tween.isKilled || !item.Value)
            {
                _retired.Add(tween);
            }
        }

        foreach (ChromaIDColorTween tween in _retired)
        {
            _active.Remove(tween);
        }

        _retired.Clear();
    }

    internal void RestartTween(ChromaIDColorTween tween, LightSwitchEventEffect owner)
    {
        float start = tween.PreviousEvent?.time ?? _audio.songTime;
        tween.SetStartTimeAndEndTime(start, start + tween.duration);
        ResumeTween(tween, owner);
    }

    internal void ResumeTween(ChromaIDColorTween tween, LightSwitchEventEffect owner)
    {
        tween.Resume();
        _active[tween] = owner;
        tween.UpdateAt(_audio.songTime);
    }
}
#endif
