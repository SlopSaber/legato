#nullable enable
using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Utilities;

namespace Legato.Presentation {
    internal static class BsmlMenuTitle {
        internal static ViewController Get() => BeatSaberUI.DiContainer.Resolve<HierarchyManager>()
            .GetField<ScreenSystem, HierarchyManager>("_screenSystem").titleViewController;
    }
}
