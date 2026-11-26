using UnityEngine;

namespace Legato.Gameplay {
    internal static class NativeGameState {
        internal static void SetPaused(PauseController controller, bool paused, bool resuming = false) {
#if LEGATO_ENUM_PAUSE_STATE
            controller._paused = resuming ? PauseController.PauseState.Resuming : paused ? PauseController.PauseState.Paused : PauseController.PauseState.Playing;
#else
            controller._paused = paused;
#endif
        }
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static bool ContinueWithZeroEnergy(StandardLevelGameplayManager.InitData data) =>
#if LEGATO_FAIL_ON_ZERO_ENERGY
            !data.failOn0Energy;
#else
            data.continueGameplayWith0Energy;
#endif
#endif
        internal static bool IsAudioPaused(AudioTimeSyncController controller) =>
#if LEGATO_SYNC_CONTROLLER_AUDIO_STATE || LEGATO_BEFORE_MULTIPLAYER
            controller.state == AudioTimeSyncController.State.Paused;
#else
            controller.state == IAudioTimeSource.State.Paused;
#endif
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static float BeatDuration(IBpmController controller) =>
#if BEAT_SABER_1_29_0
            controller == null || controller.currentBpm <= 0 ? 0f : 60f / controller.currentBpm;
#else
            controller == null ? 0f : controller.oneBeatDuration;
#endif
#endif
    }
}
