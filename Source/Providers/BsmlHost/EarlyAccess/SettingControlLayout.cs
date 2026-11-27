#if LEGATO_EARLY_VR_UI
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

internal static class EarlySettingControlLayout
{
    internal static GameObject CreateSlider(Transform parent, out RangeValuesTextSlider slider, out TextMeshProUGUI label)
    {
        var row = CreateRow(parent, "BSMLSliderSetting", out label);
        var controls = EarlyScrollLayout.CreateRect(row, "Controls");
        PlaceOnRight(controls, new Vector2(52f, 8f));
        var rect = EarlyScrollLayout.CreateRect(controls, "BSMLSlider");
        var hitArea = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0.001f);
        slider = rect.gameObject.AddComponent<RangeValuesTextSlider>();

        var track = EarlyScrollLayout.CreateRect(rect, "Track");
        track.anchorMin = new Vector2(0f, 0.5f);
        track.anchorMax = new Vector2(1f, 0.5f);
        track.offsetMin = new Vector2(2f, -1f);
        track.offsetMax = new Vector2(-2f, 1f);
        CreateImage(track, new Color(0.27f, 0.33f, 0.4f, 0.9f));

        var fill = EarlyScrollLayout.CreateRect(track, "Fill");
        CreateImage(fill, new Color(0.28f, 0.62f, 0.86f, 1f));
        slider.fillRect = fill;

        var handle = EarlyScrollLayout.CreateRect(track, "Handle");
        handle.sizeDelta = new Vector2(2f, 1.5f);
        var thumb = CreateImage(handle, Color.white);
        slider.handleRect = handle;
        slider.targetGraphic = thumb;

        var value = CreateLabel(rect, "Value");
        value.alignment = TextAlignmentOptions.Top;
        value.fontSize = 2.7f;
        var decrease = CreateStepButton(controls, false);
        var increase = CreateStepButton(controls, true);
        slider.Bind(value, decrease, increase);
        slider.SetStepButtons(false);
        return row.gameObject;
    }

    internal static SimpleTextDropdown CreateDropdown(Transform parent, out TextMeshProUGUI label)
    {
        var row = CreateRow(parent, "BSMLDropdownList", out label);
        var button = EarlyButtonFactory.Create(row, "Dropdown");
        PlaceOnRight((RectTransform)button.transform, new Vector2(51f, 7f));
        var value = EarlyButtonFactory.AddLabel(button);
        value.rectTransform.offsetMax = new Vector2(-7f, 0f);
        value.text = string.Empty;
        value.enableWordWrapping = false;
        var arrow = EarlyButtonFactory.AddIcon(button, Utilities.FindSpriteCached("ArrowDown"));
        PlaceOnRight(arrow.rectTransform, new Vector2(5f, 4f));
        arrow.rectTransform.anchoredPosition = new Vector2(-1f, 0f);

        var panel = EarlyScrollLayout.CreateRect(row, "Dropdown Choices");
        panel.gameObject.SetActive(false);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(60f, 36f);
        CreateImage(panel, new Color(0.08f, 0.11f, 0.15f, 0.98f));
        var modal = panel.gameObject.AddComponent<ModalView>();
        var table = EarlyTableLayout.Create(panel, "Options", out var tableView);
        var layout = table.GetComponent<EarlyTableLayout>();

        var dropdown = row.gameObject.AddComponent<SimpleTextDropdown>();
        dropdown.Bind(button, value, modal, tableView, EarlyListCells.CreateTextCell<EarlyDropdownCell>);
        layout.Show(dropdown, panel.sizeDelta, center: false, showScrollbar: true);
        return dropdown;
    }

    internal static GameObject CreateToggle(Transform parent, string name, out UnityEngine.UI.Toggle toggle, out TextMeshProUGUI label)
    {
        var row = CreateRow(parent, name, out label);
        label.rectTransform.offsetMax = new Vector2(-24f, 0f);

        var track = EarlyScrollLayout.CreateRect(row, "Switch");
        PlaceOnRight(track, new Vector2(22f, 7f));
        var background = track.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.sprite = Utilities.FindSpriteCached("RoundRectButton");
        background.type = UnityEngine.UI.Image.Type.Sliced;
        background.color = new Color(0.33f, 0.39f, 0.47f, 1f);

        var state = EarlyScrollLayout.CreateRect(track, "Selected");
        var foreground = CreateImage(state, new Color(0.26f, 0.64f, 0.9f, 1f));
        foreground.sprite = background.sprite;
        foreground.type = background.type;

        var stateLabel = CreateLabel(track, "Value");
        stateLabel.alignment = TextAlignmentOptions.Center;
        stateLabel.fontSize = 3.5f;
        stateLabel.text = "OFF";
        toggle = track.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        toggle.graphic = foreground;
        toggle.targetGraphic = background;
        toggle.isOn = false;
        toggle.onValueChanged.AddListener(value => stateLabel.text = value ? "ON" : "OFF");
        return row.gameObject;
    }

    internal static UnityEngine.UI.Image AddModifierIcon(Transform row, TextMeshProUGUI label)
    {
        var rect = EarlyScrollLayout.CreateRect(row, "Icon");
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(6f, 6f);
        rect.anchoredPosition = Vector2.zero;
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        var icon = CreateImage(rect, Color.white);
        icon.sprite = Utilities.FindSpriteCached("ArrowRight");
        icon.preserveAspect = true;
        return icon;
    }

    private static RectTransform CreateRow(Transform parent, string name, out TextMeshProUGUI label)
    {
        var row = EarlyScrollLayout.CreateRect(parent, name);
        row.gameObject.SetActive(false);
        row.sizeDelta = new Vector2(90f, 8f);
        var layout = row.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 90f;
        layout.preferredHeight = 8f;
        label = CreateLabel(row, "Title");
        label.rectTransform.offsetMax = new Vector2(-54f, 0f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = "Default Text";
        return row;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name)
    {
        var label = EarlyScrollLayout.CreateRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 4f;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Image CreateImage(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateStepButton(RectTransform parent, bool increase)
    {
        var button = EarlyButtonFactory.Create(parent, increase ? "Increment" : "Decrement");
        var rect = (RectTransform)button.transform;
        float edge = increase ? 1f : 0f;
        rect.anchorMin = rect.anchorMax = new Vector2(edge, 0.5f);
        rect.pivot = new Vector2(edge, 0.5f);
        rect.sizeDelta = new Vector2(5f, 5f);
        rect.anchoredPosition = Vector2.zero;
        EarlyButtonFactory.AddIcon(button, Utilities.FindSpriteCached(increase ? "ArrowRight" : "ArrowLeft"));
        return button;
    }

    private static void PlaceOnRight(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }
}
#endif
