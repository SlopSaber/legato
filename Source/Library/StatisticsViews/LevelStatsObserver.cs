#nullable enable
using SiraUtil.Affinity;

namespace Legato.UI {
    internal abstract class LevelStatsObserver : IAffinity {
#if LEGATO_BEFORE_MULTIPLAYER
        [AffinityPostfix]
        [AffinityPatch(typeof(StandardLevelDetailView), nameof(StandardLevelDetailView.RefreshContent))]
        protected void NativeStatsChanged(StandardLevelDetailView __instance, PlayerData ____playerData, bool ____showPlayerStats) {
            var key = __instance.selectedDifficultyBeatmap;
            if (!____showPlayerStats || ____playerData == null || key == null) return;
            OnStatsChanged(__instance, key, Gameplay.LevelStatisticsAccess.TryGet(____playerData, key));
        }

        protected abstract void OnStatsChanged(StandardLevelDetailView view, BeatmapKey key, PlayerLevelStatsData? stats);
#else
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
#endif
    }
}
