#nullable enable
using HMUI;
using IPA.Utilities;
using UnityEngine;

namespace Legato.Presentation {
    internal static class PauseMenuLayout {
        internal static BeatmapLevel? InitializeLevelBar(PauseMenuManager menu) {
            var data = menu != null ? menu._initData : null;
            if (data == null || !data.showLevelBar || data.beatmapLevel == null || menu!._levelBar == null || menu._levelBar._beatmapLevel != null)
                return null;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            menu._levelBar.Setup(data.beatmapLevel, data.beatmapKey.difficulty, data.beatmapKey.characteristic);
#else
            menu._levelBar.Setup(data.beatmapLevel, data.beatmapKey.difficulty, data.beatmapKey.beatmapCharacteristic);
#endif
            return data.beatmapLevel;
        }

        internal static PauseMenuManager? GetManager(PauseController controller) =>
            controller.GetField<PauseMenuManager, PauseController>("_pauseMenuManager");

        internal static Transform? FindCanvas(PauseMenuManager manager) =>
            manager.transform.Find("Wrapper/MenuWrapper/Canvas");

        internal static ImageView? FindBackground(Transform canvas) {
            var background = canvas.Find("MainBar/LevelBarSimple/BG");
            return background != null ? background.GetComponent<ImageView>() : null;
        }
    }
}
