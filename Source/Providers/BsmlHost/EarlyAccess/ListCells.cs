#if LEGATO_EARLY_VR_UI
using HMUI;
using IPA.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

internal static class EarlyListCells
{
    internal static StandardLevelListTableCell CreateSongCell(TableView table)
    {
        var cell = Create<StandardLevelListTableCell>(table, out var content);
        var cover = AddImage(content, "Cover", Color.white, false);
        var rect = cover.rectTransform;
        rect.anchorMax = new Vector2(0.15f, 1f);
        rect.offsetMin = new Vector2(1f, 1f);
        rect.offsetMax = new Vector2(-1f, -1f);
        cover.preserveAspect = true;
        cell.SetField("_coverImage", cover);
        var title = AddLabel(content, "Song", 0.17f, 0.46f, 1f);
        var author = AddLabel(content, "Author", 0.17f, 0f, 0.48f);
        author.fontSize = 2.7f;
        cell.SetField("_songNameText", title);
        cell.SetField("_authorText", author);
        return cell;
    }

    internal static SimpleListTableCell CreateTextCell(TableView table)
    {
        var cell = Create<SimpleListTableCell>(table, out var content);
        cell.SetField("_text", AddLabel(content, "Text", 0f, 0f, 1f));
        return cell;
    }

    internal static BSMLBoxTableCell CreateBoxCell(TableView table)
    {
        var node = new GameObject("BSML Box Cell", typeof(RectTransform));
        node.SetActive(false);
        node.layer = table.gameObject.layer;
        var cell = node.AddComponent<BSMLBoxTableCell>();
        var content = EarlyTableLayout.CellContent(table, cell);
        var cover = AddImage(content, "Cover", Color.white, true);
        cover.preserveAspect = true;
        var selection = AddImage(content, "Selection", Color.white, false);
        selection.color = new Color(1f, 1f, 1f, 0.25f);
        selection.enabled = false;
        cell.SetComponents(cover, selection);
        return cell;
    }

    internal static void PlaceNativeCellContents(TableView table, TableCell cell)
    {
        var original = new Transform[cell.transform.childCount];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = cell.transform.GetChild(i);
        }

        var content = EarlyTableLayout.CellContent(table, cell);
        foreach (var child in original)
        {
            child.SetParent(content, false);
        }
    }

    private static T Create<T>(TableView table, out RectTransform content) where T : TableCell
    {
        var node = new GameObject(typeof(T).Name, typeof(RectTransform));
        node.SetActive(false);
        node.layer = table.gameObject.layer;
        var cell = node.AddComponent<T>();
        content = EarlyTableLayout.CellContent(table, cell);
        AddImage(content, "Hit Area", new Color(0f, 0f, 0f, 0.001f), true);
        cell.SetField("_bgImage", AddImage(content, "Selected", new Color(0.22f, 0.55f, 0.87f, 0.7f), false));
        cell.SetField("_highlightImage", AddImage(content, "Highlight", new Color(1f, 1f, 1f, 0.22f), false));
        return cell;
    }

    private static TextMeshProUGUI AddLabel(RectTransform parent, string name, float left, float bottom, float top)
    {
        var rect = EarlyScrollLayout.CreateRect(parent, name);
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(1f, top);
        rect.offsetMin = new Vector2(1f, 0f);
        rect.offsetMax = new Vector2(-1f, 0f);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Image AddImage(Transform parent, string name, Color color, bool raycastTarget)
    {
        var image = EarlyScrollLayout.CreateRect(parent, name).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }
}
#endif
