using HMUI;
using UnityEngine;

namespace Legato.UI {
    internal abstract class MonoTableDataSource : MonoBehaviour, TableView.IDataSource {
        protected abstract float CellHeight(int index);
        public abstract int NumberOfCells();
        public abstract TableCell CellForIdx(TableView table, int index);
        public float CellSize(int index) => CellHeight(index);
#if LEGATO_EARLY_STANDARD_MAPS
        protected abstract TableView NativeTable { get; }
        public float RowHeight() => CellHeight(0);
        public int NumberOfRows() => NumberOfCells();
        public TableCell CellForRow(int row) => CellForIdx(NativeTable, row);
#elif BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE || LEGATO_BEFORE_MULTIPLAYER
        public float CellSize() => CellHeight(0);
#endif
    }

    internal abstract class TableCellLifecycle : TableCell {
#if LEGATO_MONOBEHAVIOUR_INTERACTABLE && !LEGATO_EARLY_STANDARD_MAPS
        protected virtual void OnDestroy() { }
#else
        protected override void OnDestroy() { }
#endif
    }
}
