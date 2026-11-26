using System;
using System.Linq;
using System.Reflection;

namespace Legato.Rendering {
    internal static class EnvironmentAccess {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private static readonly MethodInfo LoadEnvironment = typeof(CustomLevelLoader).GetMethod(
            "LoadEnvironmentInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(string), typeof(bool) }, null)
            ?? throw new MissingMethodException(typeof(CustomLevelLoader).FullName, "LoadEnvironmentInfo");
#else
        private static readonly FieldInfo Environments = typeof(CustomLevelLoader).GetField(
            "_environmentsListModel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(CustomLevelLoader).FullName, "_environmentsListModel");
#endif
        internal static EnvironmentInfoSO FromLoader(CustomLevelLoader loader, string name) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            (EnvironmentInfoSO)LoadEnvironment.Invoke(loader, new object[] { name, false });
#else
            ((EnvironmentsListModel)Environments.GetValue(loader)).GetEnvironmentInfoBySerializedNameSafe(name);
#endif
        internal static EnvironmentInfoSO Active(GameplayCoreSceneSetupData scene) =>
#if LEGATO_SINGLE_BEATMAP_ENVIRONMENT
            scene.environmentInfo;
#else
            scene.targetEnvironmentInfo;
#endif
        internal static EnvironmentsListModel CreateList() =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            UnityEngine.Resources.FindObjectsOfTypeAll<EnvironmentsListModel>().FirstOrDefault()
                ?? throw new InvalidOperationException("Native environment assets are not loaded.");
#else
            EnvironmentsListModel.CreateFromAddressables();
#endif

        internal static string SceneName(EnvironmentInfoSO info) =>
#if LEGATO_LEGACY_SCENE_NAMES
            info.sceneInfo.sceneName;
#else
            info.environmentSceneName;
#endif
        internal static EnvironmentInfoSO Find(EnvironmentsListModel list, string name) =>
#if LEGATO_BEFORE_MULTIPLAYER
            list.GetEnviromentInfoBySerializedName(name);
#else
            list.GetEnvironmentInfoBySerializedName(name);
#endif

        internal static int Order(EnvironmentsListModel list, EnvironmentInfoSO info) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            Array.IndexOf(list.environmentInfos, info);
#else
            info.order;
#endif

        internal static EnvironmentInfoSO Original(GameplayCoreSceneSetupData scene) =>
#if LEGATO_SINGLE_BEATMAP_ENVIRONMENT
            scene.environmentInfo;
#else
            scene.originalEnvironmentInfo;
#endif

        internal static EnvironmentInfoSO ForKey(EnvironmentsListModel environments, BeatmapLevel level, BeatmapKey key) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            key.GetEnvironmentInfo();
#else
            environments.GetEnvironmentInfoBySerializedNameSafe(level.GetEnvironmentName(in key));
#endif
    }
}
