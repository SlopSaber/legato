#if LEGATO_EARLY_VR_UI
using System;
using System.Reflection;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

public static class EarlyTableExtensions
{
    /// <summary>Creates a dormant native table and its enclosing BSML layout.</summary>
    /// <returns>The layout containing the table.</returns>
    public static GameObject CreateCustomTable(Transform parent, string name, out TableView table) =>
        EarlyTableLayout.Create(parent, name, out table);

    /// <summary>Attaches a data source and activates the native list.</summary>
    public static void ActivateCustomTable(this TableView table, TableView.IDataSource dataSource, Vector2 size, bool showScrollbar) =>
        ActivateCustomTable(table, dataSource, size, showScrollbar, false, false);

    /// <summary>Activates a custom list with the requested orientation and alignment.</summary>
    public static void ActivateCustomTable(this TableView table, TableView.IDataSource dataSource, Vector2 size, bool showScrollbar, bool alignCenter, bool horizontal)
    {
        EarlyTableLayout layout = table.transform.parent.GetComponent<EarlyTableLayout>();
        layout.SetDirection(horizontal ? "Horizontal" : "Vertical");
        layout.Show(dataSource, size, alignCenter, showScrollbar);
    }

    /// <summary>Creates a native song cell with cover, title, and author controls.</summary>
    /// <returns>The song cell ready to bind to a row.</returns>
    public static StandardLevelListTableCell CreateCustomSongCell(this TableView table) => EarlyListCells.CreateSongCell(table);

    /// <summary>Creates a native single-label cell.</summary>
    /// <returns>The text cell ready to bind to a row.</returns>
    public static SimpleListTableCell CreateCustomTextCell(this TableView table) => EarlyListCells.CreateTextCell(table);

    public static void SelectCellWithIdx(this TableView table, int index)
    {
        if (table.dataSource != null && index >= 0 && index < table.dataSource.NumberOfRows())
        {
            table.SelectRow(index);
        }
    }
}

[HarmonyPatch(typeof(TableView), "Update")]
internal static class EarlyTablePaging
{
    private static bool Prefix(TableView __instance)
    {
        if (__instance is not BSMLTableView)
        {
            return true;
        }

        EarlyTableInput input = __instance.GetComponent<EarlyTableInput>();
        return input == null || !input.IsUsingScrollRect;
    }
}

internal sealed class EarlyTableLayout : MonoBehaviour
{
    private static readonly MethodInfo RefreshCells = AccessTools.DeclaredMethod(typeof(TableView), "RefreshCells", new[] { typeof(bool) });

    private RectTransform frame;
    private RectTransform list;
    private TableView table;
    private ScrollRect scroll;
    private Button pageUp;
    private Button pageDown;
    private VerticalScrollIndicator indicator;
    private bool alignCenter;

    internal bool Horizontal { get; private set; }

    private float TotalRowHeight => table.dataSource != null ? table.dataSource.NumberOfRows() * table.dataSource.RowHeight() : 0f;

    private float ScrollTravel => Mathf.Max(0f, TotalRowHeight - list.rect.height);

    internal static GameObject Create(Transform parent, string name, out TableView table)
    {
        RectTransform frame = EarlyScrollLayout.CreateRect(parent, name + "Container");
        frame.gameObject.AddComponent<LayoutElement>();
        EarlyTableLayout layout = frame.gameObject.AddComponent<EarlyTableLayout>();
        layout.frame = frame;
        layout.list = EarlyScrollLayout.CreateRect(frame, name);
        layout.list.gameObject.SetActive(false);
        layout.scroll = layout.list.gameObject.AddComponent<ScrollRect>();
        layout.scroll.viewport = EarlyScrollLayout.CreateViewport(layout.list);
        layout.scroll.movementType = ScrollRect.MovementType.Clamped;
        layout.scroll.inertia = false;
        layout.table = layout.list.gameObject.AddComponent<BSMLTableView>();
        layout.list.gameObject.AddComponent<EarlyTableInput>().Setup(layout.table, layout.scroll);
        table = layout.table;
        return frame.gameObject;
    }

    internal static RectTransform CellContent(TableView table, TableCell cell)
    {
        EarlyTableLayout owner = table.transform.parent.GetComponent<EarlyTableLayout>();
        EarlyTableCellLayout layout = cell.GetComponent<EarlyTableCellLayout>();
        if (layout != null)
        {
            return layout.Content;
        }

        layout = cell.gameObject.AddComponent<EarlyTableCellLayout>();
        layout.Horizontal = owner != null && owner.Horizontal;
        layout.Content = EarlyScrollLayout.CreateRect(cell.transform, "BSML Cell Content");
        layout.Refresh();
        return layout.Content;
    }

    internal void SetDirection(string direction)
    {
        if (string.Equals(direction, "Vertical", StringComparison.OrdinalIgnoreCase))
        {
            Horizontal = false;
        }
        else if (string.Equals(direction, "Horizontal", StringComparison.OrdinalIgnoreCase))
        {
            Horizontal = true;
        }
        else
        {
            throw new ArgumentException("Unknown table direction " + direction, nameof(direction));
        }
    }

    internal void Show(TableView.IDataSource dataSource, Vector2 preferredSize, bool center, bool showScrollbar)
    {
        LayoutElement fit = GetComponent<LayoutElement>();
        fit.preferredWidth = preferredSize.x;
        fit.preferredHeight = preferredSize.y;
        alignCenter = center;
        if (!Horizontal && showScrollbar)
        {
            EarlyScrollLayout.AddPaging(frame, scroll.viewport, out pageUp, out pageDown, out BSMLScrollIndicator control);
            indicator = control;
            pageUp.onClick.AddListener(PageUp);
            pageDown.onClick.AddListener(PageDown);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
        ResizeList(preferredSize, dataSource);
        list.gameObject.SetActive(true);
        table.dataSource = dataSource;
    }

    internal void PageUp()
    {
        if (ScrollTravel > 0f)
        {
            table.PageScrollUp();
        }
    }

    internal void PageDown()
    {
        if (ScrollTravel > 0f)
        {
            table.PageScrollDown();
        }
    }

    private void LateUpdate()
    {
        if (list == null || !list.gameObject.activeInHierarchy || table.dataSource == null)
        {
            return;
        }

        if (ResizeList(frame.rect.size, table.dataSource))
        {
            RefreshCells.Invoke(table, new object[] { true });
        }

        if (indicator != null)
        {
            float travel = ScrollTravel;
            float p = Mathf.Clamp01(scroll.verticalNormalizedPosition);
            indicator.normalizedPageHeight = TotalRowHeight > 0f ? list.rect.height / TotalRowHeight : 1f;
            indicator.progress = travel > 0f ? 1f - p : 0f;
            pageUp.interactable = travel > 0f && p < 0.999f;
            pageDown.interactable = travel > 0f && p > 0.001f;
        }
    }

    private bool ResizeList(Vector2 size, TableView.IDataSource dataSource)
    {
        Vector2 logical = Horizontal ? new Vector2(size.y, size.x) : size;
        if (alignCenter)
        {
            logical.y = Mathf.Min(logical.y, dataSource.NumberOfRows() * dataSource.RowHeight());
        }

        if (list.anchorMin == new Vector2(0.5f, 0.5f) && list.sizeDelta == logical)
        {
            return false;
        }

        list.anchorMin = list.anchorMax = new Vector2(0.5f, 0.5f);
        list.sizeDelta = logical;
        list.localRotation = Quaternion.Euler(0f, 0f, Horizontal ? 90f : 0f);
        list.anchoredPosition = Vector2.zero;
        return true;
    }
}

internal sealed class EarlyTableCellLayout : MonoBehaviour
{
    internal RectTransform Content { get; set; }

    internal bool Horizontal { get; set; }

    internal void Refresh()
    {
        if (!Horizontal || Content == null)
        {
            return;
        }

        Rect rect = ((RectTransform)transform).rect;
        Content.anchorMin = Content.anchorMax = new Vector2(0.5f, 0.5f);
        Content.sizeDelta = new Vector2(rect.height, rect.width);
        Content.anchoredPosition = Vector2.zero;
        Content.localRotation = Quaternion.Euler(0f, 0f, -90f);
    }

    private void OnRectTransformDimensionsChange() => Refresh();
}

internal sealed class EarlyTableInput : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler
{
    private TableView table;
    private ScrollRect scroll;
    private bool dragging;
    private int inputFrame = -1;

    internal bool IsUsingScrollRect => dragging || inputFrame >= Time.frameCount - 1;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            dragging = true;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        inputFrame = Time.frameCount;
    }

    public void OnScroll(PointerEventData eventData) => inputFrame = Time.frameCount;

    internal void Setup(TableView view, ScrollRect rect)
    {
        table = view;
        scroll = rect;
    }

    private void LateUpdate()
    {
        if (IsUsingScrollRect && table.dataSource != null)
        {
            table.SetField("_targetVerticalNormalizedPosition", Mathf.Clamp01(scroll.verticalNormalizedPosition));
        }
    }

    private void OnDisable()
    {
        dragging = false;
        inputFrame = -1;
    }
}

#endif
