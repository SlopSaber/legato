using BeatmapEditor3D;
namespace Legato.Editor {
    internal static class EditorAccess {
        internal static float PlayHeadTime(UpdatePlayHeadCommand command) =>
#if BEAT_SABER_1_29_0
            AudioTimeHelper.SamplesToSeconds(command._signal.sample, command._beatmapDataModel.audioClip.frequency);
#else
            AudioTimeHelper.SamplesToSeconds(command._signal.sample, command._audioDataModel.audioClip.frequency);
#endif
    }
}
