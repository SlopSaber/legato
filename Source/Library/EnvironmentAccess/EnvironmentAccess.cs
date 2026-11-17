using System;
using System.Linq;
using System.Reflection;

namespace Legato.Rendering {
    internal static class EnvironmentAccess {
#if BEAT_SABER_1_29_0
        private static readonly MethodInfo LoadEnvironment = typeof(CustomLevelLoader).GetMethod(
            "LoadEnvironmentInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(string), typeof(bool) }, null)
            ?? throw new MissingMethodException(typeof(CustomLevelLoader).FullName, "LoadEnvironmentInfo");
#endif
        internal static EnvironmentInfoSO FromLoader(CustomLevelLoader loader, string name) =>
#if BEAT_SABER_1_29_0
            (EnvironmentInfoSO)LoadEnvironment.Invoke(loader, new object[] { name, false });
#else
            loader._environmentsListModel.GetEnvironmentInfoBySerializedNameSafe(name);
#endif
        internal static EnvironmentInfoSO Active(GameplayCoreSceneSetupData scene) =>
#if BEAT_SABER_1_29_0
            scene.environmentInfo;
#else
            scene.targetEnvironmentInfo;
#endif
        internal static EnvironmentsListModel CreateList() =>
#if BEAT_SABER_1_29_0
            UnityEngine.Resources.FindObjectsOfTypeAll<EnvironmentsListModel>().FirstOrDefault()
                ?? throw new InvalidOperationException("Native environment assets are not loaded.");
#else
            EnvironmentsListModel.CreateFromAddressables();
#endif

        internal static string SceneName(EnvironmentInfoSO info) =>
#if BEAT_SABER_1_29_0
            info.sceneInfo.sceneName;
#else
            info.environmentSceneName;
#endif

        internal static int Order(EnvironmentsListModel list, EnvironmentInfoSO info) =>
#if BEAT_SABER_1_29_0
            Array.IndexOf(list.environmentInfos, info);
#else
            info.order;
#endif

        internal static EnvironmentInfoSO Original(GameplayCoreSceneSetupData scene) =>
#if BEAT_SABER_1_29_0
            scene.environmentInfo;
#else
            scene.originalEnvironmentInfo;
#endif

        internal static EnvironmentInfoSO ForKey(EnvironmentsListModel environments, BeatmapLevel level, BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key.GetEnvironmentInfo();
#else
            environments.GetEnvironmentInfoBySerializedNameSafe(level.GetEnvironmentName(in key));
#endif
    }
}
