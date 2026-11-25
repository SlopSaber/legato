#if BEAT_SABER_1_29_0 || LEGATO_FIXED_TABLE_CELL_SIZE
namespace BeatSaberMarkupLanguage.Components
{
    /// <summary>
    /// Provides the cell size expected by the older table view.
    /// </summary>
    public partial class CustomCellListTableData
    {
        /// <inheritdoc />
        public float CellSize() => this.CellSize(0);
    }
}
#endif
