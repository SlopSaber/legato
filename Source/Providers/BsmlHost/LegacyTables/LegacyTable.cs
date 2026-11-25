#if LEGATO_HMUI_TABLE_SCROLLER
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components
{
    /// <summary>
    /// Configures the ScrollRect-backed table used by legacy HMUI.
    /// </summary>
    internal static class LegacyTable
    {
        /// <summary>
        /// Creates the inactive native table and its layout container.
        /// </summary>
        /// <param name="parent">The parent layout.</param>
        /// <param name="name">The table object name.</param>
        /// <param name="table">The native table whose data source will be assigned.</param>
        /// <returns>The layout container.</returns>
        internal static GameObject Create(Transform parent, string name, out TableView table)
        {
            GameObject container = new(name + "Container", typeof(RectTransform), typeof(LayoutElement)) { layer = 5 };
            Stretch((RectTransform)container.transform, parent);

            GameObject list = new(name, typeof(RectTransform)) { layer = 5 };
            list.SetActive(false);
            Stretch((RectTransform)list.transform, container.transform);

            ScrollRect scrollRect = list.AddComponent<ScrollRect>();
            table = list.AddComponent<BSMLTableView>();
            table._preallocatedCells = System.Array.Empty<TableView.CellsGroup>();

            GameObject viewport = new("Viewport", typeof(RectTransform), typeof(RectMask2D)) { layer = 5 };
            scrollRect.viewport = (RectTransform)viewport.transform;
            Stretch(scrollRect.viewport, list.transform);
            return container;
        }

        /// <summary>
        /// Copies the native paging buttons and indicator before the table activates.
        /// </summary>
        /// <param name="table">The table controlled by the paging buttons.</param>
        /// <param name="parent">The layout that owns the scroll bar.</param>
        /// <param name="template">The native scroll bar template.</param>
        /// <returns>The indicator that tracks the native scrolling position.</returns>
        internal static VerticalScrollIndicator AttachScrollBar(TableView table, Transform parent, TextPageScrollView template)
        {
            TextPageScrollView clone = Object.Instantiate(template);
            table._pageUpButton = clone._pageUpButton;
            table._pageDownButton = clone._pageDownButton;
            VerticalScrollIndicator indicator = clone._verticalScrollIndicator;
            RectTransform scrollBar = (RectTransform)indicator.transform.parent;
            scrollBar.SetParent(parent, false);
            scrollBar.offsetMin = Vector2.zero;
            scrollBar.offsetMax = new Vector2(8, 0);
            Object.Destroy(clone.gameObject);
            return indicator;
        }

        /// <summary>
        /// Activates and loads the table once its direction and data source are set.
        /// </summary>
        /// <param name="table">The table to load.</param>
        /// <param name="indicator">The optional scroll indicator.</param>
        internal static void Activate(TableView table, VerticalScrollIndicator indicator)
        {
            ScrollRect scrollRect = table.GetComponent<ScrollRect>();
            scrollRect.vertical = table.tableType == TableView.TableType.Vertical;
            table.gameObject.SetActive(true);
            table.ReloadData();

            if (indicator != null)
            {
                scrollRect.onValueChanged.AddListener(_ => RefreshIndicator(table, indicator));
                RefreshIndicator(table, indicator);
            }
        }

        private static void RefreshIndicator(TableView table, VerticalScrollIndicator indicator)
        {
            float pageHeight = table.scrollRectTransform.rect.height;
            float contentHeight = table.contentTransform.rect.height;
            float scrollableHeight = Mathf.Max(0f, contentHeight - pageHeight);
            indicator.normalizedPageHeight = contentHeight > 0f ? Mathf.Clamp01(pageHeight / contentHeight) : 1f;
            indicator.progress = scrollableHeight > 0f ? Mathf.Clamp01(table._scroller.position / scrollableHeight) : 0f;
        }

        private static void Stretch(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
#endif
