#if !BEAT_SABER_1_29_0
using HMUI;

namespace NalulunaUtils.BeatSaberMarkupLanguage
{
    public partial class NalulunaListTableData
    {
        float TableView.IDataSource.CellSize(int index) => CellSize(index);
    }
}
#endif
