using HMUI;
using BeatSaberMarkupLanguage.ViewControllers;

namespace Legato.UI {
    public abstract class FlowLifecycle : FlowCoordinator {
#if BEAT_SABER_1_29_0
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
#if BEAT_SABER_1_29_0
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
