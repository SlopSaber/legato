#if BEAT_SABER_1_29_0
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
