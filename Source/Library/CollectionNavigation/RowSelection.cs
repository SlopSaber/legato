namespace Legato.UI {
    internal static partial class CollectionNavigation {
        internal const string RowSelectionMethod =
#if LEGATO_SELECT_ROW_EVENT
            "HandleDidSelectRowEvent";
#else
            "HandleDidSelectCellWithIndex";
#endif
    }
}
