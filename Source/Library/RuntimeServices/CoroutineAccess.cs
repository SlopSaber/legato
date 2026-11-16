#nullable enable
using System.Collections;
using UnityEngine;

namespace Legato.Coroutines {
    internal sealed class CoroutineAccess {
#if !BEAT_SABER_1_29_0
        private readonly ICoroutineStarter _starter;
        public CoroutineAccess(ICoroutineStarter starter) { _starter = starter; }
#endif
        internal Coroutine StartCoroutine(IEnumerator routine) =>
#if BEAT_SABER_1_29_0
            global::SharedCoroutineStarter.instance.StartCoroutine(routine);
#else
            _starter.StartCoroutine(routine);
#endif
        internal void StartSingleCoroutine(ref Coroutine? routine, IEnumerator enumerator) {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(enumerator);
        }
        internal void StopCoroutine(Coroutine routine) {
#if BEAT_SABER_1_29_0
            global::SharedCoroutineStarter.instance.StopCoroutine(routine);
#else
            _starter.StopCoroutine(routine);
#endif
        }
    }
}
namespace Legato.Audio {
    internal static class PlaybackLifecycle {
        internal static bool DidStart(AudioTimeSyncController controller) =>
#if BEAT_SABER_1_29_0
            IPA.Utilities.FieldAccessor<AudioTimeSyncController, bool>.Get(controller, "_audioStarted");
#else
            controller.didStart;
#endif
    }
}
