using System.Reflection;
using HarmonyLib;
using Zenject;
#if LEGATO_PC_APP_INIT
using NativeAppInstaller = global::PCAppInit;
#elif !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
using NativeAppInstaller = global::BeatSaberInit;
#endif

namespace Legato.Injection {
    internal static class AppBindingAccess {
        internal static MethodInfo Method => AccessTools.DeclaredMethod(
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            typeof(MainSystemInit),
#else
            typeof(NativeAppInstaller),
#endif
            "InstallBindings");
        internal static DiContainer Container(object instance, object[] arguments) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            (DiContainer)arguments[0];
#else
            ((NativeAppInstaller)instance).Container;
#endif
    }
}
