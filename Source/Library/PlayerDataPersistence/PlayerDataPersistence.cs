using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;

namespace Legato.Gameplay {
    internal static class PlayerDataPersistence {
        internal static MethodInfo InitializationMethod => AccessTools.DeclaredMethod(typeof(PlayerDataModel),
#if BEAT_SABER_1_29_0
            "Load");
#else
            "Inject");
#endif

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
