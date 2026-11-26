using System.Linq;
#if !LEGATO_BEFORE_MULTIPLAYER
using IPA.Utilities;
#endif

namespace Legato.BsUtils
{
    internal static class NativeAccess
    {
#if !LEGATO_BEFORE_MULTIPLAYER
        private static readonly FieldAccessor<ScenesTransitionSetupData, SceneSetupData[]>.Accessor SceneData =
            FieldAccessor<ScenesTransitionSetupData, SceneSetupData[]>.GetAccessor("_sceneSetupDataArray");
#endif

        internal static GameplayCoreSceneSetupData GameplaySetup(ScenesTransitionSetupData setup)
        {
#if LEGATO_BEFORE_MULTIPLAYER
            return setup.sceneSetupDataArray.OfType<GameplayCoreSceneSetupData>().FirstOrDefault();
#else
            return SceneData(ref setup).OfType<GameplayCoreSceneSetupData>().FirstOrDefault();
#endif
        }
    }
}
