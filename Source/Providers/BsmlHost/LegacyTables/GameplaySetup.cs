#if LEGATO_EARLY_VR_UI
using HMUI;

namespace BeatSaberMarkupLanguage.GameplaySetup
{
    /// <summary>
    /// Supplies mod rows to the original native table view.
    /// </summary>
    public partial class GameplaySetup
    {
        /// <inheritdoc />
        public float RowHeight() => this.CellSize(0);

        /// <inheritdoc />
        public int NumberOfRows() => this.NumberOfCells();

        /// <inheritdoc />
        public TableCell CellForRow(int row) => this.CellForIdx(null, row);
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
