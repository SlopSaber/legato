#if LEGATO_EARLY_VR_UI
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

internal static class EarlySettingControlLayout
{
    internal static GameObject CreateSlider(Transform parent, out RangeValuesTextSlider slider, out TextMeshProUGUI label, float width = 90f)
    {
        RectTransform row = CreateRow(parent, "BSMLSliderSetting", out label, width);
        RectTransform controls = EarlyScrollLayout.CreateRect(row, "Controls");
        float controlsWidth = Mathf.Min(52f, width - 9f);
        PlaceOnRight(controls, new Vector2(controlsWidth, 8f));
        label.rectTransform.offsetMax = new Vector2(-controlsWidth - 2f, 0f);
        RectTransform rect = EarlyScrollLayout.CreateRect(controls, "BSMLSlider");
        UnityEngine.UI.Image hitArea = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0.001f);
        slider = rect.gameObject.AddComponent<RangeValuesTextSlider>();

        RectTransform track = EarlyScrollLayout.CreateRect(rect, "Track");
        track.anchorMin = new Vector2(0f, 0.5f);
        track.anchorMax = new Vector2(1f, 0.5f);
        track.offsetMin = new Vector2(2f, -1f);
        track.offsetMax = new Vector2(-2f, 1f);
        CreateImage(track, new Color(0.27f, 0.33f, 0.4f, 0.9f));

        RectTransform fill = EarlyScrollLayout.CreateRect(track, "Fill");
        CreateImage(fill, new Color(0.28f, 0.62f, 0.86f, 1f));
        slider.fillRect = fill;

        RectTransform handle = EarlyScrollLayout.CreateRect(track, "Handle");
        handle.sizeDelta = new Vector2(2f, 1.5f);
        UnityEngine.UI.Image thumb = CreateImage(handle, Color.white);
        slider.handleRect = handle;
        slider.targetGraphic = thumb;

        TextMeshProUGUI value = CreateLabel(rect, "Value");
        value.alignment = TextAlignmentOptions.Top;
        value.fontSize = 2.7f;
        Button decrease = CreateStepButton(controls, false);
        Button increase = CreateStepButton(controls, true);
        slider.Bind(value, decrease, increase);
        slider.SetStepButtons(false);
        return row.gameObject;
    }

    internal static SimpleTextDropdown CreateDropdown(Transform parent, out TextMeshProUGUI label)
    {
        RectTransform row = CreateRow(parent, "BSMLDropdownList", out label);
        Button button = EarlyButtonFactory.Create(row, "Dropdown");
        PlaceOnRight((RectTransform)button.transform, new Vector2(51f, 7f));
        TextMeshProUGUI value = EarlyButtonFactory.AddLabel(button);
        value.rectTransform.offsetMax = new Vector2(-7f, 0f);
        value.text = string.Empty;
        value.enableWordWrapping = false;
        LegacyImageView arrow = EarlyButtonFactory.AddIcon(button, Utilities.FindSpriteCached("ArrowDown"));
        PlaceOnRight(arrow.rectTransform, new Vector2(5f, 4f));
        arrow.rectTransform.anchoredPosition = new Vector2(-1f, 0f);

        RectTransform panel = EarlyScrollLayout.CreateRect(row, "Dropdown Choices");
        panel.gameObject.SetActive(false);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(60f, 36f);
        CreateImage(panel, new Color(0.08f, 0.11f, 0.15f, 0.98f));
        ModalView modal = panel.gameObject.AddComponent<ModalView>();
        GameObject table = EarlyTableLayout.Create(panel, "Options", out TableView tableView);
        EarlyTableLayout layout = table.GetComponent<EarlyTableLayout>();

        SimpleTextDropdown dropdown = row.gameObject.AddComponent<SimpleTextDropdown>();
        dropdown.Bind(button, value, modal, tableView, EarlyListCells.CreateTextCell<EarlyDropdownCell>);
        layout.Show(dropdown, panel.sizeDelta, center: false, showScrollbar: true);
        return dropdown;
    }

    internal static GameObject CreateToggle(Transform parent, string name, out UnityEngine.UI.Toggle toggle, out TextMeshProUGUI label)
    {
        RectTransform row = CreateRow(parent, name, out label);
        label.rectTransform.offsetMax = new Vector2(-24f, 0f);

        RectTransform track = EarlyScrollLayout.CreateRect(row, "Switch");
        PlaceOnRight(track, new Vector2(22f, 7f));
        UnityEngine.UI.Image background = track.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.sprite = Utilities.FindSpriteCached("RoundRectButton");
        background.type = UnityEngine.UI.Image.Type.Sliced;
        background.color = new Color(0.33f, 0.39f, 0.47f, 1f);

        RectTransform state = EarlyScrollLayout.CreateRect(track, "Selected");
        UnityEngine.UI.Image foreground = CreateImage(state, new Color(0.26f, 0.64f, 0.9f, 1f));
        foreground.sprite = background.sprite;
        foreground.type = background.type;

        TextMeshProUGUI stateLabel = CreateLabel(track, "Value");
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
        RectTransform rect = EarlyScrollLayout.CreateRect(row, "Icon");
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(6f, 6f);
        rect.anchoredPosition = Vector2.zero;
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        UnityEngine.UI.Image icon = CreateImage(rect, Color.white);
        icon.sprite = Utilities.FindSpriteCached("ArrowRight");
        icon.preserveAspect = true;
        return icon;
    }

    internal static GameObject CreateStepRow(Transform parent, out TextMeshProUGUI title, out TextMeshProUGUI value, out Button decrease, out Button increase)
    {
        RectTransform row = CreateRow(parent, "BSMLIncDecSetting", out title);
        title.rectTransform.offsetMax = new Vector2(-43f, 0f);

        RectTransform controls = EarlyScrollLayout.CreateRect(row, "ValuePicker");
        PlaceOnRight(controls, new Vector2(40f, 8f));
        decrease = CreateStepButton(controls, false);
        increase = CreateStepButton(controls, true);
        value = CreateLabel(controls, "Value");
        value.rectTransform.offsetMin = new Vector2(6f, 0f);
        value.rectTransform.offsetMax = new Vector2(-6f, 0f);
        value.alignment = TextAlignmentOptions.Center;
        value.overflowMode = TextOverflowModes.Ellipsis;
        return row.gameObject;
    }

    internal static GameObject CreateEditRow(Transform parent, string name, out TextMeshProUGUI title, out RectTransform controls, out Button edit)
    {
        RectTransform row = CreateRow(parent, name, out title);
        title.rectTransform.offsetMax = new Vector2(-48f, 0f);
        controls = EarlyScrollLayout.CreateRect(row, "ValuePicker");
        PlaceOnRight(controls, new Vector2(46f, 8f));
        edit = EarlyButtonFactory.Create(controls, "EditButton");
        PlaceOnRight((RectTransform)edit.transform, new Vector2(16f, 6f));
        EarlyButtonFactory.AddLabel(edit).text = "Edit";
        return row.gameObject;
    }

    internal static UnityEngine.UI.Image CreateSwatch(Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform swatch = EarlyScrollLayout.CreateRect(parent, "Color");
        swatch.anchorMin = swatch.anchorMax = new Vector2(0.5f, 0.5f);
        swatch.sizeDelta = size;
        swatch.anchoredPosition = position;
        return CreateImage(swatch, Color.white);
    }

    internal static TextMeshProUGUI CreateLabel(Transform parent, string name)
    {
        TextMeshProUGUI label = EarlyScrollLayout.CreateRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 4f;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRow(Transform parent, string name, out TextMeshProUGUI label, float width = 90f)
    {
        RectTransform row = EarlyScrollLayout.CreateRect(parent, name);
        row.gameObject.SetActive(false);
        row.sizeDelta = new Vector2(width, 8f);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 8f;
        label = CreateLabel(row, "Title");
        label.rectTransform.offsetMax = new Vector2(-54f, 0f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = "Default Text";
        return row;
    }

    private static UnityEngine.UI.Image CreateImage(RectTransform rect, Color color)
    {
        UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateStepButton(RectTransform parent, bool increase)
    {
        Button button = EarlyButtonFactory.Create(parent, increase ? "Increment" : "Decrement");
        RectTransform rect = (RectTransform)button.transform;
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
