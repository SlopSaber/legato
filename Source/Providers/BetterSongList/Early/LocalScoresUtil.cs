using System.Linq;

namespace BetterSongList.Util
{
    public static class LocalScoresUtil
    {
        public static bool hasScores => LocalLeaderboardsModel.instance != null;

        public static bool HasLocalScore(BeatmapLevel level)
        {
            var scores = LocalLeaderboardsModel.instance;
            if (scores == null || level.difficultyBeatmaps == null)
                return false;
            return level.difficultyBeatmaps.Any(difficulty =>
                scores.GetScores(LeaderboardsModel.GetLeaderboardID(difficulty, UI.FilterUI.Mode),
                    LocalLeaderboardsModel.LeaderboardType.AllTime)?.Count > 0);
        }
    }
}
