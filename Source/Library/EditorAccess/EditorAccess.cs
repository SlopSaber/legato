using BeatmapEditor3D;
namespace Legato.Editor {
    internal static class EditorAccess {
        internal static float PlayHeadTime(UpdatePlayHeadCommand command) =>
#if BEAT_SABER_1_29_0
            command._signal.time;
#else
            AudioTimeHelper.SamplesToSeconds(command._signal.sample, command._audioDataModel.audioClip.frequency);
#endif
    }
}
