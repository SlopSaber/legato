using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;

namespace Legato.UI {
    internal abstract class BsmlTableDataSource : BSMLAutomaticViewController, TableView.IDataSource {
        protected abstract float CellHeight(int index);
        public abstract int NumberOfCells();
        public abstract TableCell CellForIdx(TableView table, int index);
        public float CellSize(int index) => CellHeight(index);
#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE
        public float CellSize() => CellHeight(0);
#endif
    }
}
