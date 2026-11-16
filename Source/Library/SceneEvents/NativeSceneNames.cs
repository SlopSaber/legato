#if BEAT_SABER_1_29_0
global using SceneNames = Legato.Gameplay.LegacySceneNames;
#else
global using SceneNames = BeatSaber.Destinations.SceneNames;
#endif

#if BEAT_SABER_1_29_0
namespace Legato.Gameplay
{
    internal static class LegacySceneNames
    {
        internal const string kGameCoreSceneName = "GameCore";
        internal const string kMainMenuSceneName = "MainMenu";
    }
}
#endif
