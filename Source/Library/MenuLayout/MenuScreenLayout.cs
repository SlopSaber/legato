using HMUI;

namespace Legato.Presentation {
    internal static class MenuScreenLayout {
        internal static void SetDefaultScale(ScreenModeController controller, float scale, float height) {
            ScreenModeData data = controller._defaultModeData;
            data.position.y = height;
            data.scale = scale;
            controller.SetDefaultMode();
        }
    }
}
