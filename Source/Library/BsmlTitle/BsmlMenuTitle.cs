#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable
#if LEGATO_EARLY_STANDARD_MAPS
using System.Linq;
using UnityEngine;

namespace Legato.Presentation {
    internal static class BsmlMenuTitle {
        private const string TitleName = "LegatoMenuTitle";

        internal static RectTransform Get() {
            var navigation = UnityEngine.Resources.FindObjectsOfTypeAll<StandardLevelSelectionNavigationController>()
                .First(controller => controller.gameObject.scene.IsValid());
            if (navigation.transform.Find(TitleName) is RectTransform existing) return existing;

            var title = new GameObject(TitleName, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)title.transform;
            rect.SetParent(navigation.transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 8f);
            rect.anchoredPosition = Vector2.zero;
            rect.SetAsLastSibling();
            return rect;
        }
    }
}
#else
#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage;
#else
using BeatSaberMarkupLanguage;
#endif
using HMUI;
using IPA.Utilities;

namespace Legato.Presentation {
    internal static class BsmlMenuTitle {
        internal static ViewController Get() => BeatSaberUI.DiContainer.Resolve<HierarchyManager>()
            .GetField<ScreenSystem, HierarchyManager>("_screenSystem").titleViewController;
    }
}
#endif
