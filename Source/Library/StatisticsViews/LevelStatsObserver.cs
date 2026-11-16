#nullable enable
using SiraUtil.Affinity;

namespace Legato.UI {
    internal abstract class LevelStatsObserver : IAffinity {
        [AffinityPostfix]
        [AffinityPatch(typeof(LevelStatsView), nameof(LevelStatsView.ShowStats), AffinityMethodType.Normal,
#if BEAT_SABER_1_29_0
            new[] { typeof(IDifficultyBeatmap), typeof(PlayerData) })]
#else
            new[] { typeof(PlayerLevelStatsData) })]
#endif
        protected void NativeStatsChanged(LevelStatsView __instance, object[] __args) {
#if BEAT_SABER_1_29_0
            var key = (BeatmapKey)__args[0];
            var stats = Gameplay.LevelStatisticsAccess.TryGet((PlayerData)__args[1], key);
#else
            var stats = (PlayerLevelStatsData)__args[0];
            var key = stats.GetBeatmapKey();
#endif
            OnStatsChanged(__instance, key, stats);
        }
        protected abstract void OnStatsChanged(LevelStatsView view, BeatmapKey key, PlayerLevelStatsData? stats);
    }
}
