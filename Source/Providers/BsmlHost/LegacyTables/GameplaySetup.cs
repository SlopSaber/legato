#if BEAT_SABER_1_29_0
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
