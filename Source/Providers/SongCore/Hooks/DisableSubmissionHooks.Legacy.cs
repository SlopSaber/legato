#if LEGATO_FIXED_NOTE_JUMP_SPEED && !LEGATO_BEFORE_MULTIPLAYER
using System;
using MonoMod.RuntimeDetour;
using SiraUtil.Submissions;
using Zenject;
namespace SongCore.Hooks
{
    internal sealed class DisableSubmissionHooks : IInitializable, IDisposable
    {
        private readonly Submission _submission;
        private Hook? _playback;
#if !BEAT_SABER_1_29_0
        private Hook? _autoplay;
#endif
        private Ticket? _ticket;
        private DisableSubmissionHooks(Submission submission) => _submission = submission;
        public void Initialize()
        {
            _playback = new Hook(typeof(ObjectsMovementRecorder).GetMethod(nameof(ObjectsMovementRecorder.Init))!, Playback, true);
#if !BEAT_SABER_1_29_0
            _autoplay = new Hook(typeof(PlaybackRecord).GetProperty(nameof(PlaybackRecord.IsActive))!.GetMethod!, Autoplay, true);
#endif
        }

        public void Dispose()
        {
            _playback?.Dispose();
#if !BEAT_SABER_1_29_0
            _autoplay?.Dispose();
#endif
        }

#if !BEAT_SABER_1_29_0
        private bool Autoplay(Func<bool> original)
        {
            bool active = original();
            if (active)
                _ticket ??= _submission.DisableScoreSubmission(nameof(SongCore), "Autoplay is enabled.");
            return active;
        }
#endif

        private void Playback(Action<ObjectsMovementRecorder> original, ObjectsMovementRecorder recorder)
        {
            original(recorder);
            if (recorder._mode == ObjectsMovementRecorder.Mode.Playback)
                _ticket ??= _submission.DisableScoreSubmission(nameof(SongCore), "Playback is enabled.");
        }
    }
}
#endif
