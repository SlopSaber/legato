#if LEGATO_EARLY_SCENE_SETUP
using VRUI;
#else
using HMUI;
#endif

#if LEGATO_BEFORE_MULTIPLAYER || LEGATO_EARLY_SCENE_SETUP
using UnityEngine;

namespace Legato.Presentation {
    internal sealed class MenuScreenLayout {
        private readonly Transform _screenSystem;
        private readonly Vector3 _originalPosition;
        private readonly Vector3 _originalScale;
        private readonly float _mainScreenHeight;
#if !LEGATO_EARLY_SCENE_SETUP
        private readonly Transform _bottom;
        private readonly Vector3 _originalBottomPosition;
        private readonly Vector3 _bottomAnchor;
#endif

        private MenuScreenLayout(Transform screenSystem, Transform main) {
            _screenSystem = screenSystem;
            _originalPosition = _screenSystem.localPosition;
            _originalScale = _screenSystem.localScale;
            Vector3 mainAnchor = _screenSystem.InverseTransformPoint(main.position);
            _mainScreenHeight = (_screenSystem.localRotation * Vector3.Scale(_originalScale, mainAnchor)).y;
        }

#if LEGATO_EARLY_SCENE_SETUP
        internal static MenuScreenLayout CaptureActiveMenu() {
            MainMenuViewController menu = Object.FindObjectOfType<MainMenuViewController>();
            VRUIScreen screen = menu != null ? menu.screen : null;
            VRUIScreenSystem system = screen != null ? screen.screenSystem : null;
            return system != null && system.mainScreen == screen
                ? new MenuScreenLayout(system.transform, screen.transform) : null;
        }
#else
        private MenuScreenLayout(ScreenSystem screens) : this(screens.transform, screens.mainScreen.transform) {
            _bottom = screens.bottomScreen.transform;
            _originalBottomPosition = _bottom.localPosition;
            _bottomAnchor = _screenSystem.InverseTransformPoint(_bottom.position);
        }

        internal static MenuScreenLayout Capture(HierarchyManager hierarchy) {
            ScreenSystem screens = hierarchy._screenSystem;
            return screens != null && screens.mainScreen != null && screens.bottomScreen != null
                ? new MenuScreenLayout(screens) : null;
        }
#endif

        internal void ApplyScale(float scale) {
            if (_screenSystem == null) return;
#if !LEGATO_EARLY_SCENE_SETUP
            if (_bottom == null) return;
#endif

            // Keep the main panel at its native eye height as the whole rig scales.
            float heightShift = _mainScreenHeight * (1f - scale);
            _screenSystem.localPosition = _originalPosition + Vector3.up * heightShift;
            _screenSystem.localScale = _originalScale * scale;

#if !LEGATO_EARLY_SCENE_SETUP
            // The bottom panel keeps its original floor clearance multiplied by the scale.
            Vector3 shift = Vector3.up * heightShift;
            if (_screenSystem.parent != null)
                shift = _screenSystem.parent.TransformVector(shift);
            _bottom.position = _screenSystem.TransformPoint(_bottomAnchor) - shift;
#endif
        }

        internal void Restore() {
            if (_screenSystem != null) {
                _screenSystem.localPosition = _originalPosition;
                _screenSystem.localScale = _originalScale;
            }
#if !LEGATO_EARLY_SCENE_SETUP
            if (_bottom != null) _bottom.localPosition = _originalBottomPosition;
#endif
        }
    }
}
#else
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
#endif
