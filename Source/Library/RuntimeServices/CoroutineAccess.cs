#nullable enable
using System.Collections;
using UnityEngine;

namespace Legato.Coroutines {
    internal sealed class CoroutineAccess {
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
        private readonly ICoroutineStarter _starter;
        public CoroutineAccess(ICoroutineStarter starter) { _starter = starter; }
#endif
        internal Coroutine StartCoroutine(IEnumerator routine) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            global::SharedCoroutineStarter.instance.StartCoroutine(routine);
#else
            _starter.StartCoroutine(routine);
#endif
        internal void StartSingleCoroutine(ref Coroutine? routine, IEnumerator enumerator) {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(enumerator);
        }
        internal void StopCoroutine(Coroutine routine) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            global::SharedCoroutineStarter.instance.StopCoroutine(routine);
#else
            _starter.StopCoroutine(routine);
#endif
        }
    }
}
namespace Legato.Audio {
    internal static class PlaybackLifecycle {
        internal static bool IsPlaying(AudioTimeSyncController controller) =>
#if LEGATO_SYNC_CONTROLLER_AUDIO_STATE
            controller.state == AudioTimeSyncController.State.Playing;
#else
            controller.state == IAudioTimeSource.State.Playing;
#endif
        internal static bool DidStart(AudioTimeSyncController controller) =>
#if LEGATO_SYNC_CONTROLLER_AUDIO_STATE
            IPA.Utilities.FieldAccessor<AudioTimeSyncController, bool>.Get(controller, "_audioStarted");
#else
            controller.didStart;
#endif
    }
}
