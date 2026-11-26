#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections;
using UnityEngine;

namespace Legato.Presentation {
    internal enum LegacyMenuEase {
        Linear,
        OutCubic,
        OutQuart,
        OutElastic
    }

    internal class LegacyMenuTween {
        private readonly Action<float> _applyProgress;
        private readonly float _duration;
        private readonly float _delay;
        private readonly LegacyMenuEase _ease;
        private Coroutine _running;
        private bool _finished;
        internal Action onCompleted;
        internal Action onKilled;

        protected LegacyMenuTween(Action<float> applyProgress, float duration, LegacyMenuEase ease, float delay) {
            _applyProgress = applyProgress;
            _duration = Mathf.Max(0f, duration);
            _delay = Mathf.Max(0f, delay);
            _ease = ease;
        }

        internal void Start(UnityEngine.Object owner) {
            if (_finished || _running != null) return;
            _running = SharedCoroutineStarter.instance.StartCoroutine(Run(owner));
        }

        private IEnumerator Run(UnityEngine.Object owner) {
            float start = Time.unscaledTime + _delay;
            do {
                yield return null;
                if (owner == null) {
                    _running = null;
                    Kill();
                    yield break;
                }
            } while (Time.unscaledTime < start);

            while (!_finished) {
                float progress = _duration == 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - start) / _duration);
                _applyProgress(Ease(progress, _ease));
                if (_finished) yield break;
                if (progress >= 1f) {
                    _finished = true;
                    _running = null;
                    onCompleted?.Invoke();
                    yield break;
                }
                yield return null;
                if (owner == null) {
                    _running = null;
                    Kill();
                    yield break;
                }
            }
        }

        internal void Kill() {
            if (_finished) return;
            _finished = true;
            if (_running != null) SharedCoroutineStarter.instance.StopCoroutine(_running);
            _running = null;
            onKilled?.Invoke();
        }

        private static float Ease(float progress, LegacyMenuEase ease) {
            if (progress <= 0f || progress >= 1f) return progress;
            float remaining = 1f - progress;
            return ease switch {
                LegacyMenuEase.OutCubic => 1f - remaining * remaining * remaining,
                LegacyMenuEase.OutQuart => 1f - remaining * remaining * remaining * remaining,
                LegacyMenuEase.OutElastic => 1f + Mathf.Pow(2f, -10f * progress) * Mathf.Sin((progress * 10f - 0.75f) * (2f * Mathf.PI / 3f)),
                _ => progress
            };
        }
    }

    internal sealed class LegacyFloatTween : LegacyMenuTween {
        internal LegacyFloatTween(float from, float to, Action<float> update, float duration, LegacyMenuEase ease, float delay = 0f)
            : base(progress => update(Mathf.LerpUnclamped(from, to, progress)), duration, ease, delay) {
        }
    }

    internal sealed class LegacyVectorTween : LegacyMenuTween {
        internal LegacyVectorTween(Vector3 from, Vector3 to, Action<Vector3> update, float duration, LegacyMenuEase ease)
            : base(progress => update(Vector3.LerpUnclamped(from, to, progress)), duration, ease, 0f) {
        }
    }
}
#endif
