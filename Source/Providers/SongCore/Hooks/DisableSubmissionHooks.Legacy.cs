#if BEAT_SABER_1_29_0
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
        private Ticket? _ticket;
        private DisableSubmissionHooks(Submission submission) => _submission = submission;
        public void Initialize() => _playback = new Hook(typeof(ObjectsMovementRecorder).GetMethod(nameof(ObjectsMovementRecorder.Init))!, Playback, true);
        public void Dispose() => _playback?.Dispose();
        private void Playback(Action<ObjectsMovementRecorder> original, ObjectsMovementRecorder recorder)
        {
            original(recorder);
            if (recorder._mode == ObjectsMovementRecorder.Mode.Playback)
                _ticket ??= _submission.DisableScoreSubmission(nameof(SongCore), "Playback is enabled.");
        }
    }
}
#endif
