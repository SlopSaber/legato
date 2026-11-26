using System;
using System.Reflection;
using HMUI;
using IPA.Utilities;

namespace Legato.UI {
    internal static class ScrollViewAccess {
#if LEGATO_BEFORE_MULTIPLAYER
        internal static void ScrollToTop(TableView table, bool animated) {
            if (table.numberOfCells > 0) table.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, animated);
        }

        internal static void ScrollToBottom(TableView table, bool animated) {
            int count = table.numberOfCells;
            if (count > 0) table.ScrollToCellWithIdx(count - 1, TableViewScroller.ScrollPositionType.End, animated);
        }
#else
#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE
        private static readonly Func<TableView, ScrollView> GetScrollView = Getter<ScrollView>("scrollView");
#else
        private static readonly Func<TableView, float> GetPaddingStart = Getter<float>("paddingStart");
#endif

        private static Func<TableView, T> Getter<T>(string property) =>
            (Func<TableView, T>)Delegate.CreateDelegate(typeof(Func<TableView, T>),
                typeof(TableView).GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetGetMethod(true) ?? throw new MissingMemberException(typeof(TableView).FullName, property));

        internal static void ScrollToTop(TableView table, bool animated) {
#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE
            GetScrollView(table).ScrollTo(0, animated);
#else
            table.ScrollToPosition(0, animated);
#endif
        }
        internal static void ScrollToBottom(TableView table, bool animated) {
#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE
            GetScrollView(table).ScrollToEnd(animated);
#else
            table.ScrollToPosition(table.dataSource.NumberOfCells() * (table.cellSize + table.spacing) + GetPaddingStart(table), animated);
#endif
        }
        internal static void RefreshButtons(ScrollView view) =>
            view.InvokeMethod<object, ScrollView>("RefreshButtons");
#endif
    }
}
