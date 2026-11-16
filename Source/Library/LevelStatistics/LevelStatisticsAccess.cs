#nullable enable
using System;

namespace Legato.Gameplay {
    internal static class LevelStatisticsAccess {
        internal static PlayerLevelStatsData? TryGet(PlayerData player, BeatmapKey key) {
#if BEAT_SABER_1_29_0
            if (!key.IsValidKey()) return null;
            string id = key.LevelId();
            var characteristic = key.parentDifficultyBeatmapSet.beatmapCharacteristic;
            // The legacy getter creates missing records; a lookup must leave player data untouched.
            foreach (var stats in player.levelsStatsData) {
                if (stats.levelID == id && stats.difficulty == key.difficulty && stats.beatmapCharacteristic == characteristic)
                    return stats;
            }
            return null;
#else
            return player.TryGetPlayerLevelStatsData(key);
#endif
        }
    }
}

namespace Legato.UI {
    internal static class DifficultySelection {
        internal static BeatmapKey Key(StandardLevelDetailViewController controller) =>
#if BEAT_SABER_1_29_0
            controller.selectedDifficultyBeatmap;
#else
            controller.beatmapKey;
#endif
        internal static IDisposable Subscribe(StandardLevelDetailViewController controller, Action<StandardLevelDetailViewController> callback) =>
            new Subscription(controller, callback);

        private sealed class Subscription : IDisposable {
            private readonly StandardLevelDetailViewController _controller;
#if BEAT_SABER_1_29_0
            private readonly Action<StandardLevelDetailViewController, IDifficultyBeatmap> _handler;
#else
            private readonly Action<StandardLevelDetailViewController> _handler;
#endif
            private bool _disposed;
            internal Subscription(StandardLevelDetailViewController controller, Action<StandardLevelDetailViewController> callback) {
                _controller = controller;
#if BEAT_SABER_1_29_0
                _handler = (sender, _) => callback(sender);
#else
                _handler = callback;
#endif
                _controller.didChangeDifficultyBeatmapEvent += _handler;
            }
            public void Dispose() {
                if (_disposed) return;
                _disposed = true;
                _controller.didChangeDifficultyBeatmapEvent -= _handler;
            }
        }
    }
}
