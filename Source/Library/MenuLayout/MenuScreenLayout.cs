using HMUI;

#if LEGATO_BEFORE_MULTIPLAYER
using UnityEngine;

namespace Legato.Presentation {
    internal sealed class MenuScreenLayout {
        private readonly Transform _screenSystem;
        private readonly Transform _bottom;
        private readonly Vector3 _originalPosition;
        private readonly Vector3 _originalScale;
        private readonly Vector3 _originalBottomPosition;
        private readonly Vector3 _bottomAnchor;
        private readonly float _mainScreenHeight;

        private MenuScreenLayout(ScreenSystem screens) {
            _screenSystem = screens.transform;
            _bottom = screens.bottomScreen.transform;
            _originalPosition = _screenSystem.localPosition;
            _originalScale = _screenSystem.localScale;
            _originalBottomPosition = _bottom.localPosition;
            _bottomAnchor = _screenSystem.InverseTransformPoint(_bottom.position);
            Vector3 mainAnchor = _screenSystem.InverseTransformPoint(screens.mainScreen.transform.position);
            _mainScreenHeight = (_screenSystem.localRotation * Vector3.Scale(_originalScale, mainAnchor)).y;
        }

        internal static MenuScreenLayout Capture(HierarchyManager hierarchy) {
            ScreenSystem screens = hierarchy._screenSystem;
            return screens != null && screens.mainScreen != null && screens.bottomScreen != null
                ? new MenuScreenLayout(screens) : null;
        }

        internal void ApplyScale(float scale) {
            if (_screenSystem == null || _bottom == null) return;

            // Keep the main panel at its native eye height as the whole rig scales.
            float heightShift = _mainScreenHeight * (1f - scale);
            _screenSystem.localPosition = _originalPosition + Vector3.up * heightShift;
            _screenSystem.localScale = _originalScale * scale;

            // The bottom panel keeps its original floor clearance multiplied by the scale.
            Vector3 shift = Vector3.up * heightShift;
            if (_screenSystem.parent != null)
                shift = _screenSystem.parent.TransformVector(shift);
            _bottom.position = _screenSystem.TransformPoint(_bottomAnchor) - shift;
        }

        internal void Restore() {
            if (_screenSystem != null) {
                _screenSystem.localPosition = _originalPosition;
                _screenSystem.localScale = _originalScale;
            }
            if (_bottom != null) _bottom.localPosition = _originalBottomPosition;
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
