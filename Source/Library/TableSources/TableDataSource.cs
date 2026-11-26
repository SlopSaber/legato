using HMUI;

namespace Legato.UI {
    public abstract class TableDataSource : TableView.IDataSource {
        protected abstract float CellHeight(int index);
        public abstract int NumberOfCells();
        public abstract TableCell CellForIdx(TableView table, int index);
#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE || LEGATO_BEFORE_MULTIPLAYER
        public float CellSize() => CellHeight(0);
#else
        public float CellSize(int index) => CellHeight(index);
#endif
    }
}
