#nullable enable
#if LEGATO_BEFORE_MULTIPLAYER
using System.Linq;
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace Legato.Modifiers;

internal static class LegacyModifierUi
{
    internal static void ApplyBaseToggleStates(GameplayModifiersPanelController panel)
    {
        var model = panel._gameplayModifiersModel;
        var modifiers = panel._gameplayModifiers;
        if (modifiers == null)
            return;

        // The native setters for song speed clear the current speed when disabled.
        Apply(false);
        Apply(true);

        void Apply(bool state)
        {
            foreach (var toggle in panel._gameplayModifierToggles)
            {
                var parameters = toggle.gameplayModifier;
                if (toggle.toggle.isOn == state && model.GetModifierBoolValue(modifiers, parameters) != state)
                    model.SetModifierBoolValue(modifiers, parameters, state);
            }
        }
    }

    internal static (RectTransform Viewport, RectTransform Content, RectTransform CustomGrid) CreateLayout(
        GameplayModifiersPanelController panel)
    {
        var left = (RectTransform)panel.transform.Find("LeftColumn");
        var right = (RectTransform)panel.transform.Find("RightColumn");
        var cell = (RectTransform)panel._gameplayModifierToggles[0].transform;
        var columnHeight = Mathf.Max(left.rect.height, right.rect.height);
        var layoutWidth = Mathf.Abs(right.anchoredPosition.x - left.anchoredPosition.x) + right.rect.width;

        var viewport = MakeRect("ModifiersViewport", panel.transform);
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.anchoredPosition = Vector2.zero;
        viewport.sizeDelta = new Vector2(layoutWidth, columnHeight);
        viewport.gameObject.AddComponent<Touchable>();
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = MakeRect("ModifiersContent", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var stock = MakeRect("StockModifiers", content);
        stock.gameObject.AddComponent<LayoutElement>().preferredHeight = columnHeight;
        left.SetParent(stock, false);
        right.SetParent(stock, false);

        var custom = MakeRect("CustomModifiers", content);
        var grid = custom.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.cellSize = cell.sizeDelta;
        grid.spacing = new Vector2(Mathf.Abs(right.anchoredPosition.x - left.anchoredPosition.x) - cell.sizeDelta.x, 0.2f);
        return (viewport, content, custom);
    }

    internal static void ConfigureScrollView(ScrollView scroll, RectTransform viewport, RectTransform content)
    {
        var template = Resources.FindObjectsOfTypeAll<TextPageScrollView>().First(view =>
            view._pageUpButton != null && view._pageDownButton != null && view._verticalScrollIndicator != null);
        var bar = MakeRect("ModifiersScrollbar", viewport.parent);
        bar.anchorMin = bar.anchorMax = viewport.anchorMin;
        bar.pivot = viewport.pivot;
        bar.anchoredPosition = new Vector2(viewport.rect.width / 2f + 4.5f, 0);
        bar.sizeDelta = new Vector2(8, viewport.rect.height);

        scroll._viewport = viewport;
        scroll._contentRectTransform = content;
        scroll._smooth = template._smooth;
        scroll._pageStepRelativePosition = 0.8f;
        scroll._pageUpButton = CloneButton(template._pageUpButton, bar, 1);
        scroll._pageDownButton = CloneButton(template._pageDownButton, bar, 0);
        scroll._verticalScrollIndicator = Object.Instantiate(template._verticalScrollIndicator, bar, false);
        var indicator = (RectTransform)scroll._verticalScrollIndicator.transform;
        indicator.anchorMin = new Vector2(0.5f, 0);
        indicator.anchorMax = new Vector2(0.5f, 1);
        indicator.pivot = new Vector2(0.5f, 0.5f);
        indicator.anchoredPosition = Vector2.zero;
        indicator.sizeDelta = new Vector2(1, -16);
    }

    private static RectTransform MakeRect(string name, Transform parent)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Button CloneButton(Button source, RectTransform parent, float anchorY)
    {
        var button = Object.Instantiate(source, parent, false);
        button.onClick = new Button.ButtonClickedEvent();
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorY);
        rect.pivot = new Vector2(0.5f, anchorY);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(8, 8);
        return button;
    }
}
#endif
