using HMUI;
using IPA.Utilities;

namespace Legato.UI {
    internal static class ScrollViewAccess {
        internal static void ScrollToTop(TableView table, bool animated) {
#if BEAT_SABER_1_29_0
            table.scrollView.ScrollTo(0, animated);
#else
            table.ScrollToPosition(0, animated);
#endif
        }
        internal static void ScrollToBottom(TableView table, bool animated) {
#if BEAT_SABER_1_29_0
            table.scrollView.ScrollToEnd(animated);
#else
            table.ScrollToPosition(table.dataSource.NumberOfCells() * (table.cellSize + table.spacing) + table.paddingStart, animated);
#endif
        }
        internal static void RefreshButtons(ScrollView view) =>
            view.InvokeMethod<object, ScrollView>("RefreshButtons");
    }
}
