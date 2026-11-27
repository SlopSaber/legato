#if LEGATO_EARLY_VR_UI
using System;
using IPA.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HMUI;

public abstract class BsmlSegmentedControl : SegmentedControl, SegmentedControl.IDataSource
{
    private static TextSegmentedControlCell firstTemplate;
    private static TextSegmentedControlCell middleTemplate;
    private static TextSegmentedControlCell lastTemplate;

    public abstract int NumberOfColumns();

    public abstract SegmentedControlCell CellForColumn(int column);

    public void SelectCellWithNumber(int column)
    {
        if (column >= 0 && column < NumberOfColumns())
        {
            SelectColumn(column);
        }
    }

    internal static T Create<T>(Transform parent, string name, bool vertical = false)
        where T : BsmlSegmentedControl
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        node.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)node.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(vertical ? 12f : 60f, vertical ? 40f : 7f);
        HorizontalOrVerticalLayoutGroup layout = vertical
            ? node.AddComponent<VerticalLayoutGroup>()
            : node.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 0.35f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return node.AddComponent<T>();
    }

    protected static RectTransform CreateStretchChild(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform), typeof(LayoutElement)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    protected override void Awake()
    {
        base.Awake();
        dataSource = this;
    }

    protected TextSegmentedControlCell CreateCell(int column, string text)
    {
        if (firstTemplate == null)
        {
            foreach (TextSegmentedControlCell template in Resources.FindObjectsOfTypeAll<TextSegmentedControlCell>())
            {
                switch (template.name)
                {
                    case "FirstTextSegmentedControlCell":
                        firstTemplate = template;
                        break;
                    case "MiddleTextSegmentedControlCell":
                        middleTemplate = template;
                        break;
                    case "LastTextSegmentedControlCell":
                        lastTemplate = template;
                        break;
                }
            }
        }

        TextSegmentedControlCell prefab = column == 0 ? firstTemplate : column == NumberOfColumns() - 1 ? lastTemplate : middleTemplate;
        TextSegmentedControlCell cell = prefab != null ? Instantiate(prefab, transform, false) : CreateCellWithoutPrefab();
        cell.text = text;
        return cell;
    }

    private static UnityEngine.UI.Image AddImage(Transform parent, string name, Color color, bool raycastTarget)
    {
        UnityEngine.UI.Image image = CreateStretchChild(parent, name).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private TextSegmentedControlCell CreateCellWithoutPrefab()
    {
        GameObject node = new GameObject("BSML Segment", typeof(RectTransform));
        node.layer = gameObject.layer;
        node.SetActive(false);
        node.transform.SetParent(transform, false);
        AddImage(node.transform, "Hit Area", Color.clear, true);
        UnityEngine.UI.Image selected = AddImage(node.transform, "Selected", Color.white, false);
        UnityEngine.UI.Image highlighted = AddImage(node.transform, "Highlight", new Color(1f, 1f, 1f, 0.25f), false);
        TextMeshProUGUI label = CreateStretchChild(node.transform, "Text").gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        TextSegmentedControlCell cell = node.AddComponent<TextSegmentedControlCell>();
        cell.SetField("_text", label);
        cell.SetField("_bgImage", selected);
        cell.SetField("_highlightImage", highlighted);
        return cell;
    }
}

public sealed class TextSegmentedControl : BsmlSegmentedControl
{
    private string[] texts = Array.Empty<string>();

    public void SetTexts(string[] labels)
    {
        texts = labels ?? Array.Empty<string>();
        ReloadData();
    }

    public override int NumberOfColumns() => texts.Length;

    public override SegmentedControlCell CellForColumn(int column) => CreateCell(column, texts[column]);
}

public sealed class IconSegmentedControl : BsmlSegmentedControl
{
    private DataItem[] items = Array.Empty<DataItem>();

    public void SetData(DataItem[] data)
    {
        items = data ?? Array.Empty<DataItem>();
        ReloadData();
    }

    public override int NumberOfColumns() => items.Length;

    public override SegmentedControlCell CellForColumn(int column)
    {
        DataItem item = items[column];
        TextSegmentedControlCell cell = CreateCell(column, item.icon != null ? string.Empty : item.hintText);
        if (item.icon != null)
        {
            RectTransform rect = CreateStretchChild(cell.transform, "Icon");
            rect.sizeDelta = new Vector2(-2f, -2f);
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = item.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        return cell;
    }

    public readonly struct DataItem
    {
        public DataItem(Sprite icon, string hintText)
        {
            this.icon = icon;
            this.hintText = hintText;
        }

        public Sprite icon { get; }

        public string hintText { get; }
    }
}
#endif
