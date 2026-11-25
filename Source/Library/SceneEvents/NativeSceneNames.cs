#if LEGATO_LEGACY_SCENE_NAMES
global using SceneNames = Legato.Gameplay.LegacySceneNames;

namespace Legato.Gameplay
{
    internal static class LegacySceneNames
    {
        internal const string kGameCoreSceneName = "GameCore";
        internal const string kMainMenuSceneName = "MainMenu";
    }
}
#else
global using SceneNames = BeatSaber.Destinations.SceneNames;
#endif
