#nullable enable

using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Legato.Gameplay {
    internal static class TransitionMethods {
        internal static bool SupportsGarbageCollectionControl(MethodBase? method) => method != null && method.GetParameters().Any(parameter =>
            parameter.Name == "canTriggerGarbageCollector" &&
            (parameter.ParameterType == typeof(bool) || parameter.ParameterType == typeof(bool).MakeByRefType()));

#if LEGATO_EARLY_SCENE_SETUP
        internal static MethodInfo? StandardLevelWithBeatmapData() => AccessTools.DeclaredMethod(typeof(MenuSceneSetupData), nameof(MenuSceneSetupData.StartLevel),
            new[] { typeof(IStandardLevelDifficultyBeatmap), typeof(GameplayOptions), typeof(GameplayMode) });

        internal static MethodInfo? StandardLevelFinished() => AccessTools.DeclaredMethod(typeof(MenuSceneSetupData), "HandleMainGameSceneDidFinish",
            new[] { typeof(MainGameSceneSetupData), typeof(LevelCompletionResults) });

        internal static MethodInfo? TutorialLevelFinished() => AccessTools.DeclaredMethod(typeof(MenuSceneSetupData), "HandleTutorialSceneDidFinish",
            new[] { typeof(TutorialSceneSetupData), typeof(bool) });

        internal static MethodInfo? NativeSceneTransition() => AccessTools.DeclaredMethod(typeof(GameSceneSetupData), nameof(GameSceneSetupData.TransitionToScene),
            new[] { typeof(float) });
#else
        internal static MethodInfo? StandardLevelWithBeatmapData() => typeof(MenuTransitionsHelper).GetMethods()
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            .Where(method => method.Name == nameof(MenuTransitionsHelper.StartStandardLevel)
                && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(IDifficultyBeatmap)))
            .OrderByDescending(method => method.GetParameters().Length).FirstOrDefault();
#else
            .FirstOrDefault(method => method.Name == nameof(MenuTransitionsHelper.StartStandardLevel)
                && method.GetParameters().Any(parameter => parameter.ParameterType.FullName == "IBeatmapLevelData"));
#endif

        internal static MethodInfo? MissionLevel() => AccessTools.Method(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.StartMissionLevel));
        internal static MethodInfo? StandardLevelFinished() => AccessTools.DeclaredMethod(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.HandleMainGameSceneDidFinish));
        internal static MethodInfo? MissionLevelFinished() => AccessTools.DeclaredMethod(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.HandleMissionLevelSceneDidFinish));
        internal static MethodInfo? PushScenes() => AccessTools.Method(typeof(GameScenesManager), nameof(GameScenesManager.PushScenes));
        internal static MethodInfo? PopScenes() => AccessTools.Method(typeof(GameScenesManager), nameof(GameScenesManager.PopScenes));
        internal static MethodInfo? ReplaceScenes() => AccessTools.Method(typeof(GameScenesManager), nameof(GameScenesManager.ReplaceScenes));
#endif
    }
}
