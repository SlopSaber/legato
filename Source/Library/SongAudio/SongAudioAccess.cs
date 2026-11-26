#if LEGATO_SYNC_CONTROLLER_AUDIO_STATE
global using NativeAudioState = global::AudioTimeSyncController.State;
#else
global using NativeAudioState = global::IAudioTimeSource.State;
#endif

using IPA.Utilities;

namespace Legato.Gameplay {
    internal static class SongAudioAccess {
        internal static bool IsPlaying(AudioTimeSyncController audio) => audio.state == NativeAudioState.Playing;
#if !LEGATO_BEFORE_MULTIPLAYER
        internal static void Seek(AudioTimeSyncController audio, float songTime) {
#if LEGATO_PRIVATE_SONG_START_TIME
            float start = FieldAccessor<AudioTimeSyncController, float>.Get(audio, "_startSongTime");
#else
            float start = audio.startSongTime;
#endif
            audio.SeekTo((songTime - start) / audio.timeScale);
        }
#endif
    }
}
