#nullable enable

namespace Legato {
    internal static class GameModes {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        internal static bool RequiresPersistentCallPatch => false;
#else
        internal static bool RequiresPersistentCallPatch => true;
#endif
#if BEAT_SABER_1_45_3
        internal static GameMode Replay => GameMode.Development;
        internal static GameMode Solo => GameMode.Solo;
        internal const string ReplayName = nameof(GameMode.Development);
#else
        internal const string Replay = "Replay";
        internal const string Solo = "Solo";
        internal const string ReplayName = Replay;
#endif
        internal static bool IsParty(StandardLevelScenesTransitionSetupData data) {
#if LEGATO_BEFORE_MULTIPLAYER
            foreach (var transitions in UnityEngine.Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>()) {
                var setup = IPA.Utilities.FieldAccessor<MenuTransitionsHelper, StandardLevelScenesTransitionSetupData>
                    .Get(transitions, "_standardLevelScenesTransitionSetupData");
                if (setup != data) continue;
                var callback = IPA.Utilities.FieldAccessor<MenuTransitionsHelper, System.Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>>
                    .Get(transitions, "_standardLevelFinishedCallback");
                if (callback?.Target is PartyFreePlayFlowCoordinator) return true;
            }
            return false;
#elif BEAT_SABER_1_45_3
            return data.gameMode == GameMode.Party;
#else
            return data.gameMode?.Equals("Party", System.StringComparison.OrdinalIgnoreCase) == true;
#endif
        }

        internal static bool IsSoloOrPractice(StandardLevelScenesTransitionSetupData data) {
#if LEGATO_BEFORE_MULTIPLAYER
            var finish = IPA.Utilities.FieldAccessor<StandardLevelScenesTransitionSetupData, System.Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>>
                .Get(data, "didFinishEvent");
            if (finish == null) return false;
            foreach (var handler in finish.GetInvocationList()) {
                if (handler.Target is not MenuTransitionsHelper transitions) continue;
                var callback = IPA.Utilities.FieldAccessor<MenuTransitionsHelper, System.Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults>>
                    .Get(transitions, "_standardLevelFinishedCallback");
                if (callback?.Target is SoloFreePlayFlowCoordinator) return true;
            }
            return false;
#elif BEAT_SABER_1_45_3
            return data.gameMode is GameMode.Solo or GameMode.Practice;
#else
            return string.Equals(data.gameMode, Solo, System.StringComparison.OrdinalIgnoreCase) || IsPractice(data);
#endif
        }

#if LEGATO_BEFORE_MULTIPLAYER
        internal static StandardLevelScenesTransitionSetupData? FindStandardTransition(GameplayCoreSceneSetupData? activeSetup) {
            if (activeSetup == null) return null;
            foreach (var transition in UnityEngine.Resources.FindObjectsOfTypeAll<StandardLevelScenesTransitionSetupData>()) {
                if (ReferenceEquals(transition.Get<GameplayCoreSceneSetupData>(), activeSetup)) return transition;
            }
            return null;
        }
#endif

        internal static bool IsPractice(StandardLevelScenesTransitionSetupData data) {
#if LEGATO_BEFORE_MULTIPLAYER
            return data.Get<GameplayCoreSceneSetupData>()?.practiceSettings != null;
#elif BEAT_SABER_1_45_3
            return data.gameMode == GameMode.Practice;
#else
            return string.Equals(data.gameMode, "Practice", System.StringComparison.OrdinalIgnoreCase);
#endif
        }

        internal static string? Name(object? mode) => mode?.ToString();

#if LEGATO_DIFFICULTY_BEATMAPS || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
        internal const int ScoringTypeEra = 0;
#elif BEAT_SABER_1_40_0
        internal const int ScoringTypeEra = 1;
#else
        internal const int ScoringTypeEra = 2;
#endif
    }
}
