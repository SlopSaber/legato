using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Legato.Gameplay {
    internal static class JumpSelectionContext {
        internal static Type MovementType =>
#if BEAT_SABER_1_29_0
            typeof(BeatmapObjectSpawnMovementData);
#else
            typeof(VariableMovementDataProvider);
#endif
        internal static MethodInfo StandardSceneInit => typeof(StandardLevelScenesTransitionSetupData)
            .GetMethods().Single(method => method.Name == "Init"
                && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(GameplayModifiers))
                && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(PracticeSettings)));
        internal static (float speed, float offset) JumpDetails(BeatmapLevel level, BeatmapKey key) {
#if BEAT_SABER_1_29_0
            return key == null ? (10f, 0f) : (key.noteJumpMovementSpeed, key.noteJumpStartBeatOffset);
#else
            return BeatmapKeyExtensions.TryGetDifficultyBeatmapData(level, in key, out var data)
                ? (data.noteJumpMovementSpeed, data.noteJumpStartBeatOffset) : (10f, 0f);
#endif
        }
        internal static BeatmapKey DetailKey(StandardLevelDetailViewController view) =>
#if BEAT_SABER_1_29_0
            view.selectedDifficultyBeatmap;
#else
            view.beatmapKey;
#endif
        internal static (BeatmapKey key, BeatmapLevel level) Mission(MissionDataSO mission, BeatmapLevelsModel levels) {
#if BEAT_SABER_1_29_0
            var level = mission.level;
            var key = level.difficultyBeatmapSets
                .Where(set => set.beatmapCharacteristic == mission.beatmapCharacteristic)
                .SelectMany(set => set.difficultyBeatmaps).FirstOrDefault(map => map.difficulty == mission.beatmapDifficulty);
            return (key, level);
#else
            var key = mission.beatmapKey;
            return (key, levels.GetBeatmapLevel(key.levelId));
#endif
        }
        internal static IDisposable Subscribe(StandardLevelDetailViewController view, Action<StandardLevelDetailViewController> handler)
            => new Subscription(view, handler);
        private sealed class Subscription : IDisposable {
            private readonly StandardLevelDetailViewController _view;
#if BEAT_SABER_1_29_0
            private readonly Action<StandardLevelDetailViewController, IDifficultyBeatmap> _handler;
#else
            private readonly Action<StandardLevelDetailViewController> _handler;
#endif
            private bool _disposed;
            internal Subscription(StandardLevelDetailViewController view, Action<StandardLevelDetailViewController> handler) {
                _view = view;
#if BEAT_SABER_1_29_0
                _handler = (sender, _) => handler(sender);
#else
                _handler = handler;
#endif
                _view.didChangeDifficultyBeatmapEvent += _handler;
            }
            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
                _view.didChangeDifficultyBeatmapEvent -= _handler;
            }
        }
    }
}
