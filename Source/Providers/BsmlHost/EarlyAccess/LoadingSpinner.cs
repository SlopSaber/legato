#if LEGATO_EARLY_VR_UI
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

internal sealed class EarlyLoadingSpinner : MonoBehaviour
{
    internal static GameObject Create(Transform parent)
    {
        RectTransform rect = EarlyScrollLayout.CreateRect(parent, "BSMLLoadingIndicator");
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(8f, 8f);
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 8f;
        layout.preferredHeight = 8f;

        RectTransform art = EarlyScrollLayout.CreateRect(rect, "Spinner");
        UnityEngine.UI.Image image = art.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = Utilities.FindSpriteCached("LoadingIndicator");
        image.preserveAspect = true;
        image.raycastTarget = false;
        art.gameObject.AddComponent<EarlyLoadingSpinner>();
        return rect.gameObject;
    }

    private void Update() => transform.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);
}
#endif
