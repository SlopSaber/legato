#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Legato.UI {
    internal static class LevelDetailAccess {
#if BEAT_SABER_1_29_0
        private static readonly Type RefreshState = AccessTools.DeclaredMethod(typeof(StandardLevelDetailView), "SetContentForBeatmapDataAsync")
            .GetCustomAttribute<AsyncStateMachineAttribute>().StateMachineType;
        private static readonly FieldInfo Owner = AccessTools.Field(RefreshState, "<>4__this");
        private static readonly FieldInfo State = AccessTools.Field(RefreshState, "<>1__state");
#endif
        internal static MethodBase RefreshMethod =>
#if BEAT_SABER_1_29_0
            AccessTools.DeclaredMethod(RefreshState, "MoveNext");
#else
            AccessTools.DeclaredMethod(typeof(StandardLevelDetailView), "CheckIfBeatmapLevelDataExists");
#endif
        internal static StandardLevelDetailView? CompletedView(object instance) =>
#if BEAT_SABER_1_29_0
            (int)State.GetValue(instance) == -2 ? (StandardLevelDetailView)Owner.GetValue(instance) : null;
#else
            (StandardLevelDetailView)instance;
#endif
        internal static BeatmapLevel Level(StandardLevelDetailView view) =>
#if BEAT_SABER_1_29_0
            view._level;
#else
            view._beatmapLevel;
#endif
        internal static BeatmapKey Key(StandardLevelDetailView view) =>
#if BEAT_SABER_1_29_0
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static bool Precalculated(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            !level.levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId, StringComparison.Ordinal);
#else
            level.hasPrecalculatedData;
#endif
    }
}
