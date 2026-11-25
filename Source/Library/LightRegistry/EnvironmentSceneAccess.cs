using System;
using System.Reflection;

namespace Legato.Lighting
{
    internal static class EnvironmentSceneAccess
    {
        private static readonly Func<GameScenesManager, string, bool> Contains =
            (Func<GameScenesManager, string, bool>)typeof(GameScenesManager).GetMethod(
                "IsSceneInStack",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null)!.CreateDelegate(typeof(Func<GameScenesManager, string, bool>));

        internal static bool ContainsScene(GameScenesManager manager, string sceneName) => Contains(manager, sceneName);

        internal static string SerializedName(EnvironmentSceneSetupData setup) =>
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            setup.environmentSerializedName;
#else
            setup.environmentInfo.serializedName;
#endif
    }
}
