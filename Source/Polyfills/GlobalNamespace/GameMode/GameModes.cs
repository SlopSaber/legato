#nullable enable

namespace Legato {
    internal static class GameModes {
#if BEAT_SABER_1_29_0
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
#if BEAT_SABER_1_45_3
            return data.gameMode == GameMode.Party;
#else
            return data.gameMode?.Equals("Party", System.StringComparison.OrdinalIgnoreCase) == true;
#endif
        }

        internal static string? Name(object? mode) => mode?.ToString();

#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
        internal const int ScoringTypeEra = 0;
#elif BEAT_SABER_1_40_0
        internal const int ScoringTypeEra = 1;
#else
        internal const int ScoringTypeEra = 2;
#endif
    }
}
