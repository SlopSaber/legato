#nullable enable

using LeaderboardCore.Models;

namespace Legato.Leaderboards {
    internal abstract class CustomLeaderboardAdapter : CustomLeaderboard {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        public sealed override bool ShowForLevel(IDifficultyBeatmap? selectedLevel) => selectedLevel?.level != null && ShowForLevelId(selectedLevel.level.levelID);
#else
        public sealed override bool ShowForLevel(BeatmapKey? beatmapKey) => beatmapKey.HasValue && ShowForLevelId(beatmapKey.Value.levelId);
#endif

        protected abstract bool ShowForLevelId(string levelId);
    }
}
