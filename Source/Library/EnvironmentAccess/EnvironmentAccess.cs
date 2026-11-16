using System;
using System.Linq;

namespace Legato.Rendering {
    internal static class EnvironmentAccess {
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
