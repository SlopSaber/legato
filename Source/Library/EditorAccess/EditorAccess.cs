using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
namespace Legato.Editor {
    internal static class EditorAccess {
        internal static float PlayHeadTime(UpdatePlayHeadCommand command) =>
#if BEAT_SABER_1_29_0
            command._signal.sample / (float)command._beatmapDataModel.audioClip.frequency;
#else
            command._signal.sample / (float)command._audioDataModel.audioClip.frequency;
#endif
    }
}
