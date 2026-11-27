#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using BeatSaberMarkupLanguage.FloatingScreen;
using UnityEngine;
using VRUI;
using Object = UnityEngine.Object;

namespace Legato.UI;

internal sealed class EarlyLeaderboardScreen : IDisposable
{
    private readonly VRUIViewController _host;
    private readonly Transform[] _nativeChildren;
    private readonly CanvasGroup _nativeContent;
    private readonly FloatingScreen _navigation;
    private readonly FloatingScreen _panel;
    private readonly FloatingScreen _scores;
    private Transform? _navigationParent;
    private Transform? _panelParent;
    private Transform? _scoresParent;

    internal EarlyLeaderboardScreen(VRUIViewController host, VRUIViewController navigation)
    {
        _host = host;
        RectTransform parent = host.rectTransform;
        _nativeChildren = new Transform[parent.childCount];
        for (int index = 0; index < _nativeChildren.Length; index++) _nativeChildren[index] = parent.GetChild(index);

        GameObject content = new("LeaderboardCore Native Contents", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform layout = (RectTransform)content.transform;
        layout.SetParent(parent, false);
        layout.anchorMin = Vector2.zero;
        layout.anchorMax = Vector2.one;
        layout.pivot = parent.pivot;
        layout.offsetMin = layout.offsetMax = Vector2.zero;
        layout.SetSiblingIndex(0);
        foreach (Transform child in _nativeChildren) child.SetParent(layout, false);
        _nativeContent = content.GetComponent<CanvasGroup>();

        float width = parent.rect.width;
        float height = parent.rect.height;
        _scores = CreateScreen(parent, "LeaderboardCore Scores", new Vector2(width, height), 0f);
        _panel = CreateScreen(parent, "LeaderboardCore Panel", new Vector2(width, 18f), (height - 18f) * 0.5f);
        _navigation = CreateScreen(parent, "LeaderboardCore Pages", new Vector2(width, 16f), (height - 16f) * 0.5f);
        SetView(_navigation, navigation, ref _navigationParent);
    }

    internal static bool IsNativeLeaderboard(VRUIScreen screen) => screen.screenSystem != null && screen == screen.screenSystem.rightScreen;

    internal bool IsActive => _host && _host.isActiveAndEnabled && _host.screen && _host.screen.rootViewController == _host;

    internal void ShowNative(bool show)
    {
        if (!_nativeContent) return;
        _nativeContent.alpha = show ? 1f : 0f;
        _nativeContent.interactable = show;
        _nativeContent.blocksRaycasts = show;
    }

    internal void Show(VRUIViewController? panel, VRUIViewController? scores)
    {
        SetView(_panel, panel, ref _panelParent);
        SetView(_scores, scores, ref _scoresParent);
    }

    internal void Hide(VRUIViewController? panel, VRUIViewController? scores)
    {
        if (_panel && _panel.rootViewController == panel) SetView(_panel, null, ref _panelParent);
        if (_scores && _scores.rootViewController == scores) SetView(_scores, null, ref _scoresParent);
    }

    public void Dispose()
    {
        SetView(_navigation, null, ref _navigationParent);
        Show(null, null);
        if (_host && _nativeContent)
        {
            Transform parent = _host.transform;
            for (int index = 0; index < _nativeChildren.Length; index++)
            {
                Transform child = _nativeChildren[index];
                if (child && child.parent == _nativeContent.transform)
                {
                    child.SetParent(parent, false);
                    child.SetSiblingIndex(index);
                }
            }
        }
        RemoveOwned(_nativeContent);
        RemoveOwned(_navigation);
        RemoveOwned(_panel);
        RemoveOwned(_scores);
    }

    private static FloatingScreen CreateScreen(RectTransform parent, string name, Vector2 size, float y)
    {
        FloatingScreen screen = FloatingScreen.CreateFloatingScreen(size, false, Vector3.zero, Quaternion.identity);
        screen.name = name;
        RectTransform layout = (RectTransform)screen.transform;
        layout.SetParent(parent, false);
        layout.anchorMin = layout.anchorMax = new Vector2(0.5f, 0.5f);
        layout.anchoredPosition3D = new Vector3(0f, y, 0f);
        layout.localScale = Vector3.one;
        return screen;
    }

    private static void SetView(FloatingScreen screen, VRUIViewController? view, ref Transform? previousParent)
    {
        if (!screen || screen.rootViewController == view) return;

        VRUIViewController previous = screen.rootViewController;
        screen.SetRootViewController(null);
        if (previous) previous.transform.SetParent(previousParent, false);
        previousParent = view ? view!.transform.parent : null;
        if (view) screen.SetRootViewController(view);
    }

    private static void RemoveOwned(Component component)
    {
        if (!component) return;
        component.gameObject.SetActive(false);
        component.transform.SetParent(null, false);
        Object.Destroy(component.gameObject);
    }
}
#endif
