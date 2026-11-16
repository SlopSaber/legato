#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Zenject;

namespace SongCore.Hooks
{
    internal sealed class V4LightingHooks : IInitializable, IDisposable
    {
        private readonly bool _enabled;
        private readonly AudioTimeSyncController _audio;
        private readonly IBpmController _bpm;
        private readonly BeatmapCallbacksController _callbacks;
        private readonly List<Hook> _hooks = new();
        private readonly ConditionalWeakTable<LightColorGroupEffect, Single> _single = new();
        private readonly ConditionalWeakTable<BufferedLightColorGroupEffect, Buffered> _buffered = new();
        private readonly List<Single> _activeSingle = new();

        private V4LightingHooks(IDifficultyBeatmap difficulty, AudioTimeSyncController audio, IBpmController bpm, BeatmapCallbacksController callbacks)
        {
            _enabled = V4LoadingHooks.IsV4(difficulty);
            _audio = audio; _bpm = bpm; _callbacks = callbacks;
        }

        public void Initialize()
        {
            if (!_enabled) return;
            Add(typeof(LightColorGroupEffect), "HandleColorChangeBeatmapEvent", (Action<Action<LightColorGroupEffect, LightColorBeatmapEventData>, LightColorGroupEffect, LightColorBeatmapEventData>)HandleSingle);
            Add(typeof(LightColorGroupEffect), "SetColor", (Action<Action<LightColorGroupEffect, float>, LightColorGroupEffect, float>)SetSingleColor);
            Add(typeof(BufferedLightColorGroupEffect), "HandleColorChangeBeatmapEvent", (Action<Action<BufferedLightColorGroupEffect, LightColorBeatmapEventData>, BufferedLightColorGroupEffect, LightColorBeatmapEventData>)HandleBuffered);
            Add(typeof(BufferedLightColorGroupEffect), "HandleBeatmapCallbacksControllerDidProcessAllCallbacksThisFrame", (Action<Action<BufferedLightColorGroupEffect>, BufferedLightColorGroupEffect>)FlushBuffered);
            Add(typeof(BeatmapCallbacksController), nameof(BeatmapCallbacksController.ManualUpdate), (Action<Action<BeatmapCallbacksController, float>, BeatmapCallbacksController, float>)UpdateCallbacks);
        }

        private void Add(Type type, string method, Delegate handler) => _hooks.Add(new Hook(type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, handler, true));

        public void Dispose()
        {
            for (int index = _hooks.Count - 1; index >= 0; index--) _hooks[index].Dispose();
            _hooks.Clear(); _activeSingle.Clear();
        }

        private void HandleSingle(Action<LightColorGroupEffect, LightColorBeatmapEventData> original, LightColorGroupEffect effect, LightColorBeatmapEventData current)
        {
            if (current is V4LightingData.Event && NeedsAdvanced(current))
            {
                if (!_single.TryGetValue(effect, out Single state))
                {
                    state = new Single(effect);
                    _single.Add(effect, state); _activeSingle.Add(state);
                }
                state.Current = current; state.Bpm = _bpm.currentBpm; state.Dirty = true;
            }
            else if (_single.TryGetValue(effect, out Single previous)) previous.Current = null;
            original(effect, current);
        }

        private void SetSingleColor(Action<LightColorGroupEffect, float> original, LightColorGroupEffect effect, float progress)
        {
            if (_single.TryGetValue(effect, out Single state) && state.Current != null) RenderSingle(state, _audio.songTime);
            else original(effect, progress);
        }

        private void UpdateCallbacks(Action<BeatmapCallbacksController, float> original, BeatmapCallbacksController callbacks, float songTime)
        {
            original(callbacks, songTime);
            if (ReferenceEquals(callbacks, _callbacks))
                foreach (Single state in _activeSingle) if (state.Current != null) RenderSingle(state, songTime);
        }

        private static void RenderSingle(Single state, float time)
        {
            bool boost = state.Effect._usingBoostColors;
            if (!state.Dirty && state.Boost == boost && (!state.Animated || state.Time == time)) return;
            Color color = Sample(state.Current!, state.GetColor, boost, state.Bpm, time, out bool animated);
            state.Effect._lightManager.SetColorForId(state.Effect._lightId, color);
            state.Time = time; state.Animated = animated; state.Boost = boost; state.Dirty = false;
        }

        private void HandleBuffered(Action<BufferedLightColorGroupEffect, LightColorBeatmapEventData> original, BufferedLightColorGroupEffect effect, LightColorBeatmapEventData current)
        {
            if (current is V4LightingData.Event || _buffered.TryGetValue(effect, out _))
            {
                Buffered state = _buffered.GetValue(effect, static value => new Buffered(value));
                if (current.elementId >= 0 && current.elementId < state.Events.Length)
                {
                    state.Events[current.elementId] = current;
                    state.Bpms[current.elementId] = _bpm.currentBpm;
                    state.Dirty = true;
                    state.Advanced |= NeedsAdvanced(current);
                }
            }
            original(effect, current);
        }

        private void FlushBuffered(Action<BufferedLightColorGroupEffect> original, BufferedLightColorGroupEffect effect)
        {
            if (_buffered.TryGetValue(effect, out Buffered state) && state.Advanced && (state.Dirty || state.Animated || state.Boost != effect._useBoostColors))
            {
                float time = _audio.songTime;
                int count = 0;
                bool animated = false;
                for (int element = 0; element < state.Events.Length; element++)
                {
                    LightColorBeatmapEventData? current = state.Events[element];
                    if (current == null) continue;
                    int index = count++ % effect._colorsBuffer.Length;
                    effect._colorsBuffer[index] = Sample(current, state.GetColor, effect._useBoostColors, state.Bpms[element], time, out bool active);
                    effect._timesBuffer[index] = time;
                    effect._elementIdsBuffer[index] = element;
                    animated |= active;
                }
                effect._lastIndex = count % effect._colorsBuffer.Length;
                effect._didReceiveEventThisFrame |= count > 0;
                state.Animated = animated; state.Dirty = false; state.Boost = effect._useBoostColors;
            }
            original(effect);
        }

        private static Color Sample(LightColorBeatmapEventData current, Func<EnvironmentColorType, bool, float, Color> getColor, bool boost, float bpm, float time, out bool animated)
        {
            var details = (current as V4LightingData.Event)?.Details;
            var next = current.nextSameTypeEventData as LightColorBeatmapEventData;
            var nextDetails = (next as V4LightingData.Event)?.Details;
            bool interpolate = next != null && (nextDetails?.Ease ?? ((int)next.transitionType == 1 ? EaseType.Linear : EaseType.None)) != EaseType.None;
            float duration = next == null ? 1000f : Mathf.Max(0, next.time - current.time);
            float elapsed = Mathf.Max(0, time - current.time);
            float progress = duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
            Color color = getColor(current.colorType, boost, current.brightness);
            float frequency = current.strobeBeatFrequency * bpm / 60f;
            float toFrequency = frequency;
            float brightness = details?.StrobeBrightness ?? 0;
            if (interpolate)
            {
                float eased = Interpolation.Interpolate(progress, nextDetails?.Ease ?? EaseType.Linear);
                color = Color.Lerp(color, getColor(next!.colorType, boost, next.brightness), eased);
                toFrequency = next.strobeBeatFrequency * bpm / 60f;
                brightness = Mathf.LerpUnclamped(brightness, nextDetails?.StrobeBrightness ?? 0, progress);
            }
            bool strobe = frequency > 0 || toFrequency > 0;
            if (strobe)
            {
                float phase = (frequency * elapsed + (toFrequency - frequency) * (duration <= 0 ? 0 : elapsed * elapsed / (2f * duration))) % 1f;
                Color low = color; low.a = brightness;
                if (details?.StrobeFade == true)
                {
                    float wave = 1f - Mathf.Abs(phase * 2f - 1f);
                    color = Color.LerpUnclamped(color, low, Easing.InOutCubic(wave));
                }
                else if (phase > 0.5f) color = low;
            }
            animated = strobe || (interpolate && progress < 1);
            return color;
        }

        private static bool NeedsAdvanced(LightColorBeatmapEventData current)
        {
            var next = current.nextSameTypeEventData as LightColorBeatmapEventData;
            var details = (current as V4LightingData.Event)?.Details;
            var nextDetails = (next as V4LightingData.Event)?.Details;
            return current.strobeBeatFrequency > 0 || (next?.strobeBeatFrequency ?? 0) > 0
                || (details?.StrobeBrightness ?? 0) != 0 || details?.StrobeFade == true
                || (nextDetails != null && nextDetails.Ease != EaseType.None && nextDetails.Ease != EaseType.Linear);
        }

        private sealed class Single
        {
            internal readonly LightColorGroupEffect Effect;
            internal readonly Func<EnvironmentColorType, bool, float, Color> GetColor;
            internal LightColorBeatmapEventData? Current;
            internal float Bpm, Time;
            internal bool Dirty, Boost, Animated;
            internal Single(LightColorGroupEffect effect) { Effect = effect; GetColor = effect.GetColor; }
        }

        private sealed class Buffered
        {
            internal readonly LightColorBeatmapEventData?[] Events;
            internal readonly float[] Bpms;
            internal readonly Func<EnvironmentColorType, bool, float, Color> GetColor;
            internal bool Dirty, Boost, Animated, Advanced;
            internal Buffered(BufferedLightColorGroupEffect effect)
            {
                Events = new LightColorBeatmapEventData?[effect._lightColorBeatmapEventCallbackWrappers.Length];
                Bpms = new float[Events.Length]; GetColor = effect.GetColor;
            }
        }
    }
}
#endif
