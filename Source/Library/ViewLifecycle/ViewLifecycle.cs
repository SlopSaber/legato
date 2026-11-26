using HMUI;
using BeatSaberMarkupLanguage.ViewControllers;

namespace Legato.UI {
    public abstract class FlowLifecycle : FlowCoordinator {
#if LEGATO_HMUI_ACTIVATION_ENUMS
        public override void DidActivate(bool first, ActivationType activationType) =>
            OnActivated(first, activationType == ActivationType.AddedToHierarchy, false);
        public override void BackButtonWasPressed(ViewController view) => OnBack(view);
        protected void SetTitle(string value) => title = value;
#elif LEGATO_HMUI_VIEW_LIFECYCLE
        public override void DidActivate(bool first, bool added, bool enabling) => OnActivated(first, added, enabling);
        public override void BackButtonWasPressed(ViewController view) => OnBack(view);
#else
        protected override void DidActivate(bool first, bool added, bool enabling) => OnActivated(first, added, enabling);
        protected override void BackButtonWasPressed(ViewController view) => OnBack(view);
#endif
        protected abstract void OnActivated(bool first, bool added, bool enabling);
        protected abstract void OnBack(ViewController view);
    }
    public abstract class ResourceViewLifecycle : BSMLResourceViewController {
#if LEGATO_HMUI_VIEW_LIFECYCLE && !LEGATO_HMUI_ACTIVATION_ENUMS
        public override void DidDeactivate(bool removed, bool disabling) {
#else
        protected override void DidDeactivate(bool removed, bool disabling) {
#endif
            base.DidDeactivate(removed, disabling);
            OnDeactivated(removed, disabling);
        }
        protected abstract void OnDeactivated(bool removed, bool disabling);
    }
}
