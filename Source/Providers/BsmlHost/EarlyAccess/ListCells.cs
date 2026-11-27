#if LEGATO_EARLY_VR_UI
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using TMPro;
using UnityEngine;

namespace BeatSaberMarkupLanguage.Components;

internal static class EarlyListCells
{
    internal static StandardLevelListTableCell CreateSongCell(TableView table)
    {
        StandardLevelListTableCell cell = Create<StandardLevelListTableCell>(table, out RectTransform content);
        UnityEngine.UI.Image cover = AddImage(content, "Cover", Color.white, false);
        RectTransform rect = cover.rectTransform;
        rect.anchorMax = new Vector2(0.15f, 1f);
        rect.offsetMin = new Vector2(1f, 1f);
        rect.offsetMax = new Vector2(-1f, -1f);
        cover.preserveAspect = true;
        cell.SetField("_coverImage", cover);
        TextMeshProUGUI title = AddLabel(content, "Song", 0.17f, 0.46f, 1f);
        TextMeshProUGUI author = AddLabel(content, "Author", 0.17f, 0f, 0.48f);
        author.fontSize = 2.7f;
        cell.SetField("_songNameText", title);
        cell.SetField("_authorText", author);
        return cell;
    }

    internal static SimpleListTableCell CreateTextCell(TableView table)
        => CreateTextCell<SimpleListTableCell>(table);

    internal static T CreateTextCell<T>(TableView table)
        where T : SimpleListTableCell
    {
        T cell = Create<T>(table, out RectTransform content);
        SimpleListTableCell textCell = cell;
        textCell.SetField("_text", AddLabel(content, "Text", 0f, 0f, 1f));
        return cell;
    }

    internal static BSMLBoxTableCell CreateBoxCell(TableView table)
    {
        GameObject node = new GameObject("BSML Box Cell", typeof(RectTransform));
        node.SetActive(false);
        node.layer = table.gameObject.layer;
        BSMLBoxTableCell cell = node.AddComponent<BSMLBoxTableCell>();
        RectTransform content = EarlyTableLayout.CellContent(table, cell);
        UnityEngine.UI.Image cover = AddImage(content, "Cover", Color.white, true);
        cover.preserveAspect = true;
        UnityEngine.UI.Image selection = AddImage(content, "Selection", Color.white, false);
        selection.color = new Color(1f, 1f, 1f, 0.25f);
        selection.enabled = false;
        cell.SetComponents(cover, selection);
        return cell;
    }

    internal static void PlaceNativeCellContents(TableView table, TableCell cell)
    {
        Transform[] original = new Transform[cell.transform.childCount];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = cell.transform.GetChild(i);
        }

        RectTransform content = EarlyTableLayout.CellContent(table, cell);
        foreach (Transform child in original)
        {
            child.SetParent(content, false);
        }
    }

    private static T Create<T>(TableView table, out RectTransform content)
        where T : TableCell
    {
        GameObject node = new GameObject(typeof(T).Name, typeof(RectTransform));
        node.SetActive(false);
        node.layer = table.gameObject.layer;
        T cell = node.AddComponent<T>();
        content = EarlyTableLayout.CellContent(table, cell);
        AddImage(content, "Hit Area", new Color(0f, 0f, 0f, 0.001f), true);
        AccessTools.Field(typeof(T), "_bgImage").SetValue(cell, AddImage(content, "Selected", new Color(0.22f, 0.55f, 0.87f, 0.7f), false));
        AccessTools.Field(typeof(T), "_highlightImage").SetValue(cell, AddImage(content, "Highlight", new Color(1f, 1f, 1f, 0.22f), false));
        return cell;
    }

    private static TextMeshProUGUI AddLabel(RectTransform parent, string name, float left, float bottom, float top)
    {
        RectTransform rect = EarlyScrollLayout.CreateRect(parent, name);
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(1f, top);
        rect.offsetMin = new Vector2(1f, 0f);
        rect.offsetMax = new Vector2(-1f, 0f);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Image AddImage(Transform parent, string name, Color color, bool raycastTarget)
    {
        UnityEngine.UI.Image image = EarlyScrollLayout.CreateRect(parent, name).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }
}
#endif
