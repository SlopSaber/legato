#nullable enable

using System.Reflection;
using System.Threading.Tasks;

namespace Legato.Gameplay {
    internal static class PlayerDataPersistence {
        private const BindingFlags DeclaredMethods = BindingFlags.DeclaredOnly | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static MethodInfo InitializationMethod => typeof(PlayerDataModel).GetMethod(
#if BEAT_SABER_1_29_0
            "Load",
#else
            "Inject",
#endif
            DeclaredMethods)!;

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
