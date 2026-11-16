using System.Linq;
using IPA.Utilities;

namespace Legato.BsUtils
{
    internal static class NativeAccess
    {
        private static readonly FieldAccessor<ScenesTransitionSetupData, SceneSetupData[]>.Accessor SceneData =
            FieldAccessor<ScenesTransitionSetupData, SceneSetupData[]>.GetAccessor("_sceneSetupDataArray");

        internal static GameplayCoreSceneSetupData GameplaySetup(ScenesTransitionSetupData setup)
        {
            return SceneData(ref setup).OfType<GameplayCoreSceneSetupData>().FirstOrDefault();
        }
    }
}
