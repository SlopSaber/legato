using System.Reflection;
using HarmonyLib;
using Zenject;
#if !BEAT_SABER_1_29_0
using BGLib.AppFlow;
#endif

namespace Legato.Injection {
    internal static class AppBindingAccess {
        internal static MethodInfo Method => AccessTools.DeclaredMethod(
#if BEAT_SABER_1_29_0
            typeof(MainSystemInit),
#else
            typeof(BeatSaberInit),
#endif
            "InstallBindings");
        internal static DiContainer Container(object instance, object[] arguments) =>
#if BEAT_SABER_1_29_0
            (DiContainer)arguments[0];
#else
            ((BeatSaberInit)instance).Container;
#endif
    }
}
