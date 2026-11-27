#if LEGATO_EARLY_VR_UI
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

internal static class EarlyScrollLayout
{
    internal static T Create<T>(Transform parent, string name, bool showPaging)
        where T : ScrollView
    {
        RectTransform root = CreateRect(parent, name);
        root.gameObject.SetActive(false);
        T view = root.gameObject.AddComponent<T>();
        ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
        RectTransform viewport = CreateViewport(root);
        RectTransform content = CreateRect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        view.Bind(scrollRect, viewport, content);
        if (showPaging)
        {
            AddPaging(root, viewport, out Button up, out Button down, out BSMLScrollIndicator indicator);
            view.BindPaging(up, down);
            view.Indicator = indicator;
        }

        return view;
    }

    internal static RectTransform CreateViewport(Transform parent)
    {
        RectTransform viewport = CreateRect(parent, "Viewport");
        viewport.gameObject.AddComponent<RectMask2D>();
        UnityEngine.UI.Image hitArea = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0.001f);
        return viewport;
    }

    internal static GameObject CreateContents(ScrollView view, bool controlHeight)
    {
        GameObject content = view.Content.gameObject;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = controlHeight;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ExternalComponents external = content.AddComponent<ExternalComponents>();
        external.Components.Add(view);
        external.Components.Add(view.transform);
        external.Components.Add(view.gameObject.AddComponent<LayoutElement>());
        view.gameObject.SetActive(true);
        return content;
    }

    internal static void AddPaging(RectTransform root, RectTransform viewport, out Button up, out Button down, out BSMLScrollIndicator indicator)
    {
        viewport.offsetMax = new Vector2(-7f, 0f);
        RectTransform rail = CreateRect(root, "Scroll Bar");
        rail.anchorMin = new Vector2(1f, 0f);
        rail.anchorMax = Vector2.one;
        rail.pivot = new Vector2(1f, 0.5f);
        rail.sizeDelta = new Vector2(6f, 0f);
        rail.anchoredPosition = Vector2.zero;

        up = CreatePageButton(rail, "Page Up", true);
        down = CreatePageButton(rail, "Page Down", false);
        indicator = CreateIndicator(rail);
        RectTransform marker = (RectTransform)indicator.transform;
        marker.anchorMin = new Vector2(0.32f, 0f);
        marker.anchorMax = new Vector2(0.68f, 1f);
        marker.offsetMin = new Vector2(0f, 6f);
        marker.offsetMax = new Vector2(0f, -6f);
    }

    internal static BSMLScrollIndicator CreateIndicator(Transform parent)
    {
        RectTransform track = CreateRect(parent, "BSMLScrollIndicator");
        UnityEngine.UI.Image background = track.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(1f, 1f, 1f, 0.12f);
        background.raycastTarget = false;
        RectTransform handle = CreateRect(track, "Handle");
        UnityEngine.UI.Image image = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 1f, 1f, 0.85f);
        image.raycastTarget = false;
        BSMLScrollIndicator indicator = track.gameObject.AddComponent<BSMLScrollIndicator>();
        indicator.Handle = handle;
        return indicator;
    }

    internal static RectTransform CreateRect(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static Button CreatePageButton(Transform parent, string name, bool up)
    {
        Button button = EarlyButtonFactory.Create(parent, name);
        RectTransform rect = (RectTransform)button.transform;
        float edge = up ? 1f : 0f;
        rect.anchorMin = new Vector2(0f, edge);
        rect.anchorMax = new Vector2(1f, edge);
        rect.pivot = new Vector2(0.5f, edge);
        rect.sizeDelta = new Vector2(0f, 5f);
        rect.anchoredPosition = Vector2.zero;
        EarlyButtonFactory.AddIcon(button, Utilities.FindSpriteCached(up ? "ArrowUp" : "ArrowDown"));
        return button;
    }
}
#endif
