#if LEGATO_EARLY_VR_UI
using System;
using System.Collections.Generic;
using IPA.Utilities;
using UnityEngine;
using VRUI;

namespace BeatSaberMarkupLanguage.Settings;

internal static class EarlySettingsNavigation
{
    internal static MainSettingsMenuViewController GetMenu(SettingsViewController settings)
    {
        return settings.GetField<MainSettingsMenuViewController, SettingsViewController>("_mainSettingsMenuViewController");
    }

    internal static void AddEntry(MainSettingsMenuViewController menu, VRUIViewController view, string title)
    {
        SettingsSubMenuInfo[] entries = menu.GetField<SettingsSubMenuInfo[], MainSettingsMenuViewController>("_settingsSubMenuInfos");
        SettingsSubMenuInfo entry = new();
        entry.SetField("_controller", view);
        entry.SetField("_menuName", title);

        SettingsSubMenuInfo[] extended = new SettingsSubMenuInfo[entries.Length + 1];
        Array.Copy(entries, extended, entries.Length);
        extended[entries.Length] = entry;
        menu.SetField("_settingsSubMenuInfos", extended);
    }

    internal static void ConfigurePanel(VRUIViewController view)
    {
        RectTransform rect = view.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(104f, 68f);
    }

    internal static void PlacePage(VRUIViewController view, RectTransform parent)
    {
        RectTransform rect = view.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
}

internal sealed class EarlySettingsPageStack
{
    private readonly Stack<(VRUIViewController View, Transform Home)> pages = new();
    private readonly RectTransform parent;

    internal EarlySettingsPageStack(RectTransform parent)
    {
        this.parent = parent;
    }

    internal VRUIViewController Current => pages.Count > 0 ? pages.Peek().View : null;

    internal void Push(VRUIViewController view, bool isSubmenu)
    {
        VRUIViewController previous = Current;
        Transform home = isSubmenu ? view.transform.parent : null;
        EarlySettingsNavigation.PlacePage(view, parent);
        if (previous != null)
        {
            previous.gameObject.SetActive(false);
        }

        pages.Push((view, home));
        view.gameObject.SetActive(true);
    }

    internal void Pop()
    {
        HidePage(pages.Pop());
        VRUIViewController previous = Current;
        if (previous != null)
        {
            previous.gameObject.SetActive(true);
        }
    }

    internal void Clear()
    {
        while (pages.Count > 0)
        {
            HidePage(pages.Pop());
        }
    }

    private static void HidePage((VRUIViewController View, Transform Home) page)
    {
        if (page.View == null)
        {
            return;
        }

        page.View.gameObject.SetActive(false);
        if (page.Home != null)
        {
            page.View.transform.SetParent(page.Home, false);
        }
    }
}
#endif
