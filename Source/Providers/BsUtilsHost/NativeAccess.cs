using System.Linq;
#if LEGATO_BSUTILS_EARLY
using HarmonyLib;
#elif !LEGATO_BEFORE_MULTIPLAYER
using IPA.Utilities;
#endif

namespace Legato.BsUtils
{
    internal static class NativeAccess
    {
#if LEGATO_BSUTILS_EARLY
        private static readonly AccessTools.FieldRef<MainFlowCoordinator, MenuSceneSetupData> MenuData =
            AccessTools.FieldRefAccess<MainFlowCoordinator, MenuSceneSetupData>("_menuSceneSetupData");

        internal static MenuSceneSetupData GetMenuSetup(MainFlowCoordinator coordinator) =>
            coordinator != null ? MenuData(coordinator) : null;
#endif
#if !LEGATO_BEFORE_MULTIPLAYER
        private static readonly FieldAccessor<ScenesTransitionSetupData, SceneSetupData[]>.Accessor SceneData =
            FieldAccessor<ScenesTransitionSetupData, SceneSetupData[]>.GetAccessor("_sceneSetupDataArray");
#endif

        internal static GameplayCoreSceneSetupData GameplaySetup(ScenesTransitionSetupData setup)
        {
#if LEGATO_BSUTILS_EARLY
            return setup as MainGameSceneSetupData;
#elif LEGATO_BEFORE_MULTIPLAYER
            return setup.sceneSetupDataArray.OfType<GameplayCoreSceneSetupData>().FirstOrDefault();
#else
            return SceneData(ref setup).OfType<GameplayCoreSceneSetupData>().FirstOrDefault();
#endif
        }
    }
}
