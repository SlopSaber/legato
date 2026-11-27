#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
using UnityEngine;
#if LEGATO_EARLY_STANDARD_MAPS
using NativeDspOffset = DoubleSO;
#else
using NativeDspOffset = DoubleVariableSetter;
#endif
#elif LEGATO_SYNC_CONTROLLER_AUDIO_STATE || LEGATO_BEFORE_MULTIPLAYER
global using NativeAudioState = global::AudioTimeSyncController.State;
#else
global using NativeAudioState = global::IAudioTimeSource.State;
#endif

using IPA.Utilities;

namespace Legato.Gameplay {
    internal static class SongAudioAccess {
#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
        internal static bool IsPlaying(AudioTimeSyncController audio) =>
            audio != null && audio.isActiveAndEnabled &&
            FieldAccessor<AudioTimeSyncController, AudioSource>.Get(audio, "_audioSource") is AudioSource source &&
            source != null && source.isPlaying;

        internal static double DspTimeOffset(AudioTimeSyncController audio) =>
            FieldAccessor<AudioTimeSyncController, NativeDspOffset>.Get(audio, "_dspTimeOffset").value;

#if LEGATO_EARLY_STANDARD_MAPS
        internal static void Seek(AudioTimeSyncController audio, float songTime) {
            if (!IsPlaying(audio)) return;

            AudioSource source = FieldAccessor<AudioTimeSyncController, AudioSource>.Get(audio, "_audioSource");
            AudioClip clip = source.clip;
            if (clip == null) return;

            float offset = FieldAccessor<AudioTimeSyncController, float>.Get(audio, "_songTimeOffset");
            ref float start = ref FieldAccessor<AudioTimeSyncController, float>.GetAccessor("_startSongTime")(ref audio);
            float original = start;
            // StartSong sets the DSP offset and sample cursor together with the clip and gameplay clocks.
            start = Mathf.Clamp(songTime + offset, 0f, Mathf.Max(0f, clip.length - 0.01f)) - offset;
            try {
                audio.StopSong();
                audio.StartSong();
            }
            finally {
                start = original;
            }
        }
#endif
#else
        internal static bool IsPlaying(AudioTimeSyncController audio) => audio.state == NativeAudioState.Playing;
        internal static void Seek(AudioTimeSyncController audio, float songTime) {
#if LEGATO_BEFORE_MULTIPLAYER
            float target = UnityEngine.Mathf.Clamp(songTime, 0f, UnityEngine.Mathf.Max(0f, audio.songEndTime - 0.01f));
            var original = FieldAccessor<AudioTimeSyncController, AudioTimeSyncController.InitData>.Get(audio, "_initData");
            bool paused = audio.state == AudioTimeSyncController.State.Paused;
            audio.StopSong();
            audio.SetField("_initData", new AudioTimeSyncController.InitData(original.audioClip, target, original.songTimeOffset, original.timeScale));
            try {
                audio.StartSong();
                if (paused) audio.Pause();
            }
            finally {
                audio.SetField("_initData", original);
            }
#else
#if LEGATO_PRIVATE_SONG_START_TIME
            float start = FieldAccessor<AudioTimeSyncController, float>.Get(audio, "_startSongTime");
#else
            float start = audio.startSongTime;
#endif
            audio.SeekTo((songTime - start) / audio.timeScale);
#endif
        }
#endif
    }
}
