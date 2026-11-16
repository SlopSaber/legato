using IPA.Utilities;

namespace Legato.Gameplay {
    internal static class SongAudioAccess {
        internal static bool IsPlaying(AudioTimeSyncController audio) =>
#if BEAT_SABER_1_29_0
            audio.state == AudioTimeSyncController.State.Playing;
#else
            audio.state == IAudioTimeSource.State.Playing;
#endif
        internal static void Seek(AudioTimeSyncController audio, float songTime) {
#if BEAT_SABER_1_29_0
            float start = FieldAccessor<AudioTimeSyncController, float>.Get(audio, "_startSongTime");
#else
            float start = audio.startSongTime;
#endif
            audio.SeekTo((songTime - start) / audio.timeScale);
        }
    }
}
