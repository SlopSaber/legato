using UnityEngine;

namespace Legato.Gameplay {
    internal static class NativeGameState {
        internal static void SetPaused(PauseController controller, bool paused, bool resuming = false) {
#if BEAT_SABER_1_29_0
            controller._paused = paused;
#else
            controller._paused = resuming ? PauseController.PauseState.Resuming : paused ? PauseController.PauseState.Paused : PauseController.PauseState.Playing;
#endif
        }
        internal static bool ContinueWithZeroEnergy(StandardLevelGameplayManager.InitData data) =>
#if BEAT_SABER_1_29_0
            !data.failOn0Energy;
#else
            data.continueGameplayWith0Energy;
#endif
        internal static bool IsAudioPaused(AudioTimeSyncController controller) =>
#if BEAT_SABER_1_29_0
            controller.state == AudioTimeSyncController.State.Paused;
#else
            controller.state == IAudioTimeSource.State.Paused;
#endif
        internal static float BeatDuration(IBpmController controller) =>
#if BEAT_SABER_1_29_0
            controller == null || controller.currentBpm <= 0 ? 0f : 60f / controller.currentBpm;
#else
            controller == null ? 0f : controller.oneBeatDuration;
#endif
    }
}
