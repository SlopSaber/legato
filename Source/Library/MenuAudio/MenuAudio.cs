using IPA.Utilities;
using UnityEngine;

namespace Legato.Audio {
    internal static class MenuAudio {
        internal static AudioClip DefaultClip(SongPreviewPlayer player) => player.GetField<AudioClip, SongPreviewPlayer>("_defaultAudioClip");
        internal static void ProcessResultDestination(ResultsViewController controller) {
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
            if (controller._menuDestinationRequest != null)
                controller.ProcessMenuDestinationRequest(controller._menuDestinationRequest);
#endif
        }
    }
}
