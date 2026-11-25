using System.Linq;

namespace Legato.Scenes {
    internal static class SceneNames {
        internal static string[] FromSetup(LevelScenesTransitionSetupData setup) =>
#if LEGATO_LEGACY_SCENE_NAMES
            setup.scenes?.Select(scene => scene.sceneName).ToArray();
#else
            setup.scenes;
#endif
    }
}
