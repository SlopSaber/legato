#if LEGATO_EARLY_VR_UI
using HMUI;

namespace BeatSaberMarkupLanguage.GameplaySetup
{
    public partial class GameplaySetup
    {
        public float RowHeight() => CellSize(0);
        public int NumberOfRows() => NumberOfCells();
        public TableCell CellForRow(int row) => CellForIdx(null, row);
    }
}
#elif BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE
namespace BeatSaberMarkupLanguage.GameplaySetup
{
    /// <summary>
    /// Provides the gameplay settings cell size for the older table view.
    /// </summary>
    public partial class GameplaySetup
    {
        /// <inheritdoc />
        public float CellSize() => this.CellSize(0);
    }
}
#endif
