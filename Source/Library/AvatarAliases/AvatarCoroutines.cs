using System.Collections;
using UnityEngine;
#if !BEAT_SABER_1_29_0
using BeatSaber.BeatAvatarAdapter.AvatarEditor;
#endif

namespace Legato.Avatars {
    internal static class AvatarCoroutines {
        internal static Coroutine Start(AvatarTweenController controller, IEnumerator routine) =>
#if BEAT_SABER_1_29_0
            global::SharedCoroutineStarter.instance.StartCoroutine(routine);
#else
            controller._sharedCoroutineStarter.StartCoroutine(routine);
#endif
        internal static void Stop(AvatarTweenController controller, Coroutine routine) {
#if BEAT_SABER_1_29_0
            global::SharedCoroutineStarter.instance.StopCoroutine(routine);
#else
            controller._sharedCoroutineStarter.StopCoroutine(routine);
#endif
        }
    }
}
