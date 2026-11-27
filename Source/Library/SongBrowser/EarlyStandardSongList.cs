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
            (RectTransform)ViewTable(view).transform.parent;

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

        internal static bool OpenReplacement(StandardLevelListViewController view, IStandardLevel[] source,
            IStandardLevel previous, IStandardLevel updated)
        {
            if (view == null || view.selectedLevel != previous || ViewLevels(view) is not { } visible)
                return false;

            int row = Array.IndexOf(visible, previous);
            int sourceRow = Array.IndexOf(source, previous);
            if (row < 0 || sourceRow < 0 ||
                source.GetType().GetElementType()?.IsInstanceOfType(updated) != true)
                return false;

            var replacement = new IStandardLevel[visible.Length];
            Array.Copy(visible, replacement, visible.Length);
            replacement[row] = updated;
            if (!Replace(view, replacement))
                return false;

            // SongCore and PlaylistManager keep this array to rebuild the game menu after playback.
            source[sourceRow] = updated;
            return TrySelect(view, row);
        }
    }
}
