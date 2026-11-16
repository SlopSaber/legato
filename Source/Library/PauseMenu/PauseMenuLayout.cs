#nullable enable
using HMUI;
using IPA.Utilities;
using UnityEngine;

namespace Legato.Presentation {
    internal static class PauseMenuLayout {
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
