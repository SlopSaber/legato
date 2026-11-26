#if LEGATO_BEFORE_MULTIPLAYER
using UnityEngine;

namespace Legato.Gameplay {
    internal static class LegacyReplayClock {
        internal static bool IsReady(AudioTimeSyncController controller) =>
            controller.state != AudioTimeSyncController.State.Stopped && controller.isAudioLoaded;

        internal static float ClampTime(AudioTimeSyncController controller, float time) {
            var start = controller._initData.startSongTime;
            var lastSample = controller.songEndTime - 1f / controller._initData.audioClip.frequency;
            return Mathf.Clamp(time, start, Mathf.Max(start, lastSample));
        }

        internal static void SeekPaused(AudioTimeSyncController controller, float songTime, float speed) {
            var original = controller._initData;
            controller.StopSong();
            controller._initData = new AudioTimeSyncController.InitData(original.audioClip, songTime, original.songTimeOffset, speed);
            try {
                controller.StartSong();
                controller.Pause();
                PrepareResume(controller);
            } finally {
                controller._initData = original;
                controller._startSongTime = original.startSongTime;
            }
        }

        internal static void PrepareResume(AudioTimeSyncController controller) {
            var clipTime = controller.songTime + controller._songTimeOffset;
            var speed = controller.timeScale;
            controller._audioStartTimeOffsetSinceStart = Time.timeSinceLevelLoad * speed - clipTime;
            controller._dspTimeOffset = AudioSettings.dspTime - clipTime / (double)speed;
        }
    }
}
#endif
