using System.Threading.Tasks;

namespace Legato.Gameplay {
    internal static class PlayerDataPersistence {
        internal static Task Save(PlayerDataModel model) {
#if BEAT_SABER_1_29_0
            model.Save();
            return Task.CompletedTask;
#else
            return model.SaveAsync();
#endif
        }
    }
}
