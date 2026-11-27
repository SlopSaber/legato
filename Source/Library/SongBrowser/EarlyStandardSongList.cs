using System;
using HarmonyLib;
using HMUI;
using UnityEngine;

namespace Legato.SongBrowser
{
    internal static class EarlyStandardSongList
    {
        private static readonly AccessTools.FieldRef<StandardLevelListViewController, StandardLevelListTableView> ViewTable =
            AccessTools.FieldRefAccess<StandardLevelListViewController, StandardLevelListTableView>("_levelListTableView");
        private static readonly AccessTools.FieldRef<StandardLevelListTableView, TableView> NativeTable =
            AccessTools.FieldRefAccess<StandardLevelListTableView, TableView>("_tableView");
        private static readonly AccessTools.FieldRef<StandardLevelListViewController, IStandardLevel[]> ViewLevels =
            AccessTools.FieldRefAccess<StandardLevelListViewController, IStandardLevel[]>("_levels");
        private static readonly AccessTools.FieldRef<StandardLevelListTableView, IStandardLevel[]> TableLevels =
            AccessTools.FieldRefAccess<StandardLevelListTableView, IStandardLevel[]>("_levels");

        internal static RectTransform Container(StandardLevelListViewController view) =>
            ViewTable(view).transform.parent as RectTransform;

        internal static bool Replace(StandardLevelListViewController view, IStandardLevel[] levels)
        {
            if (view == null || !view.isInViewControllerHierarchy)
                return false;

            StandardLevelListTableView wrapper = ViewTable(view);
            TableView table = NativeTable(wrapper);
            if (!ReferenceEquals(table.dataSource, wrapper))
                return false;

            // Both the table and the controller dereference the selected row.
            ViewLevels(view) = levels;
            TableLevels(wrapper) = levels;
            var rect = (RectTransform)wrapper.transform;
            float padding = Mathf.Max(0f, (Container(view).rect.height - levels.Length * wrapper.RowHeight()) * 0.5f);
            rect.offsetMin = new Vector2(rect.offsetMin.x, padding);
            rect.offsetMax = new Vector2(rect.offsetMax.x, -padding);
            table.ReloadData();
            IStandardLevel selected = view.selectedLevel;
            int index = selected == null ? -1 : Array.IndexOf(levels, selected);
            if (index >= 0)
            {
                table.SelectRow(index);
                table.ScrollToRow(index, false);
            }
            else if (levels.Length > 0)
            {
                table.ScrollToRow(0, false);
            }
            return true;
        }

        internal static bool TrySelect(StandardLevelListViewController view, int index)
        {
            if (view == null || !view.isInViewControllerHierarchy || ViewLevels(view) is not { } levels ||
                (uint)index >= (uint)levels.Length)
                return false;
            var table = NativeTable(ViewTable(view));
            table.SelectRow(index, true);
            table.ScrollToRow(index, false);
            return true;
        }

        internal static void ScrollTo(StandardLevelListViewController view, int index)
        {
            if (view != null && view.isInViewControllerHierarchy && ViewLevels(view) is { } levels &&
                (uint)index < (uint)levels.Length)
                NativeTable(ViewTable(view)).ScrollToRow(index, false);
        }
    }
}
