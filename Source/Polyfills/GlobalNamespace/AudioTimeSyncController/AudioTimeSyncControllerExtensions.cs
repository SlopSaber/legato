#nullable enable

#if LEGATO_AUDIO_WITHOUT_STATE
using System.Reflection;
#endif
#if BEAT_SABER_0_10_1
using UnityEngine;
#endif

namespace Legato {
    internal static class AudioTimeSyncControllerExtensions {
#if LEGATO_AUDIO_WITHOUT_STATE
        private static readonly FieldInfo PauseField = typeof(AudioTimeSyncController)
            .GetField("_pause", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
#endif
#if BEAT_SABER_0_10_1
        private static readonly FieldInfo AudioSourceField = typeof(AudioTimeSyncController)
            .GetField("_audioSource", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
#endif

        internal static bool IsPlaying(this AudioTimeSyncController controller) {
#if BEAT_SABER_0_10_1
            return AudioSourceField.GetValue(controller) is AudioSource source && source != null && source.isPlaying;
#elif LEGATO_AUDIO_WITHOUT_STATE
            return controller.enabled && !controller.IsPaused();
#elif LEGATO_DIFFICULTY_BEATMAPS || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || (BEAT_SABER_1_40_0 && !LEGATO_AUDIO_SOURCE_STATE)
            return controller.state == AudioTimeSyncController.State.Playing;
#else
            return controller.state == IAudioTimeSource.State.Playing;
#endif
        }

        internal static bool IsPaused(this AudioTimeSyncController controller) {
#if LEGATO_AUDIO_WITHOUT_STATE
            return (bool)PauseField.GetValue(controller);
#elif LEGATO_DIFFICULTY_BEATMAPS || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || (BEAT_SABER_1_40_0 && !LEGATO_AUDIO_SOURCE_STATE)
            return controller.state == AudioTimeSyncController.State.Paused;
#else
            return controller.state == IAudioTimeSource.State.Paused;
#endif
        }
    }
}
