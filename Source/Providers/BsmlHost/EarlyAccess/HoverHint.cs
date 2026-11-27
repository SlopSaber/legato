#if LEGATO_EARLY_VR_UI
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

internal sealed class EarlyHoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform popup;
    private TextMeshProUGUI label;
    private string hint;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        RectTransform owner = (RectTransform)transform;
        RectTransform container = (RectTransform)canvas.transform;
        if (popup == null)
        {
            popup = EarlyScrollLayout.CreateRect(container, "BSML Hover Hint");
            popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0.5f);
            LayoutElement layout = popup.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            UnityEngine.UI.Image background = popup.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.08f, 0.09f, 0.12f, 0.97f);
            background.raycastTarget = false;
            label = EarlySettingControlLayout.CreateLabel(popup, "Hint");
            label.fontSize = 3.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
        }

        label.text = hint;
        const float maxWidth = 48f;
        Vector2 textSize = label.GetPreferredValues(hint, maxWidth - 4f, 0f);
        popup.sizeDelta = new Vector2(Mathf.Min(maxWidth, textSize.x + 4f), textSize.y + 3f);
        Rect ownerBounds = owner.rect;
        Vector3 above = container.InverseTransformPoint(owner.TransformPoint(new Vector3(ownerBounds.center.x, ownerBounds.yMax, 0f)));
        Vector3 below = container.InverseTransformPoint(owner.TransformPoint(new Vector3(ownerBounds.center.x, ownerBounds.yMin, 0f)));
        Vector2 half = popup.sizeDelta * 0.5f;
        Rect bounds = container.rect;
        float x = Mathf.Clamp(above.x, bounds.xMin + half.x, bounds.xMax - half.x);
        float y = above.y + half.y + 1f;
        if (y + half.y > bounds.yMax)
        {
            y = below.y - half.y - 1f;
        }

        popup.localPosition = new Vector3(x, Mathf.Clamp(y, bounds.yMin + half.y, bounds.yMax - half.y), 0f);
        popup.SetAsLastSibling();
        popup.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) => Hide();

    internal static void Attach(GameObject owner, string text)
    {
        EarlyHoverHint component = owner.GetComponent<EarlyHoverHint>();
        if (component == null)
        {
            component = owner.AddComponent<EarlyHoverHint>();
        }

        component.hint = text;
    }

    private void OnDisable() => Hide();

    private void OnDestroy()
    {
        if (popup != null)
        {
            Destroy(popup.gameObject);
        }
    }

    private void Hide()
    {
        if (popup != null)
        {
            popup.gameObject.SetActive(false);
        }
    }
}
#endif
