using System.Linq;

namespace Legato.Scenes {
    internal static class SceneNames {
        internal static string[] FromSetup(LevelScenesTransitionSetupData setup) =>
#if BEAT_SABER_1_29_0
            setup.scenes?.Select(scene => scene.sceneName).ToArray();
#else
            setup.scenes;
#endif
    }
}
