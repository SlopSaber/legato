#if LEGATO_EARLY_VR_UI
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

internal static class EarlyButtonFactory
{
    internal static Button Create(Transform parent, string name)
    {
        var node = new GameObject(name, typeof(RectTransform));
        node.layer = parent.gameObject.layer;
        var rect = (RectTransform)node.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(20f, 6f);
        var background = node.AddComponent<LegacyImageView>();
        background.sprite = Utilities.FindSpriteCached("RoundRectButton");
        background.type = Image.Type.Sliced;
        var button = node.AddComponent<Button>();
        button.targetGraphic = background;
        return button;
    }

    internal static TextMeshProUGUI AddLabel(Button button)
    {
        var label = CreateContent(button.transform, "Text").gameObject.AddComponent<TextMeshProUGUI>();
        label.font = BeatSaberUI.MainTextFont ?? TMP_Settings.defaultFontAsset;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 4f;
        label.text = "Default Text";
        label.richText = true;
        label.raycastTarget = false;
        return label;
    }

    internal static LegacyImageView AddIcon(Button button, Sprite sprite)
    {
        var rect = CreateContent(button.transform, "Icon");
        rect.sizeDelta = new Vector2(-2f, -2f);
        var icon = rect.gameObject.AddComponent<LegacyImageView>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
    }

    private static RectTransform CreateContent(Transform parent, string name)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }
}
#endif
