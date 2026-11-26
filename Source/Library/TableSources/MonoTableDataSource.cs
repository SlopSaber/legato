using HMUI;
using UnityEngine;

namespace Legato.UI {
    internal abstract class MonoTableDataSource : MonoBehaviour, TableView.IDataSource {
        protected abstract float CellHeight(int index);
        public abstract int NumberOfCells();
        public abstract TableCell CellForIdx(TableView table, int index);
        public float CellSize(int index) => CellHeight(index);
#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE || LEGATO_BEFORE_MULTIPLAYER
        public float CellSize() => CellHeight(0);
#endif
    }

    internal abstract class TableCellLifecycle : TableCell {
#if LEGATO_MONOBEHAVIOUR_INTERACTABLE
        protected virtual void OnDestroy() { }
#else
        protected override void OnDestroy() { }
#endif
    }
}
