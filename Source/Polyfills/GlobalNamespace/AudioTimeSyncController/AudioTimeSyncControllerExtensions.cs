#nullable enable

#if LEGATO_EARLY_ACCESS
using System.Reflection;
#endif

namespace Legato {
    internal static class AudioTimeSyncControllerExtensions {
#if LEGATO_EARLY_ACCESS
        private static readonly FieldInfo PauseField = typeof(AudioTimeSyncController)
            .GetField("_pause", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
#endif

        internal static bool IsPlaying(this AudioTimeSyncController controller) {
#if LEGATO_EARLY_ACCESS
            return controller.enabled && !controller.IsPaused();
#elif LEGATO_DIFFICULTY_BEATMAPS || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0
            return controller.state == AudioTimeSyncController.State.Playing;
#else
            return controller.state == IAudioTimeSource.State.Playing;
#endif
        }

        internal static bool IsPaused(this AudioTimeSyncController controller) {
#if LEGATO_EARLY_ACCESS
            return (bool)PauseField.GetValue(controller);
#elif LEGATO_DIFFICULTY_BEATMAPS || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0
            return controller.state == AudioTimeSyncController.State.Paused;
#else
            return controller.state == IAudioTimeSource.State.Paused;
#endif
        }
    }
}
