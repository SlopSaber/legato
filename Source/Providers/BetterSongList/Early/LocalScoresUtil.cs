using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace BetterSongList.Util
{
    public static class LocalScoresUtil
    {
        private static readonly AccessTools.FieldRef<PlayerDynamicData, List<PlayerLevelStatsData>> Stats =
            AccessTools.FieldRefAccess<PlayerDynamicData, List<PlayerLevelStatsData>>("_playerLevelStatsData");
        private static readonly HashSet<string> PlayedSoloLevels = new(StringComparer.Ordinal);

        public static bool hasScores => UI.FilterUI.Mode.IsSolo()
            ? GameDataModel.instance != null
            : LocalLeaderboardsModel.instance != null;

        internal static void Load()
        {
            PlayedSoloLevels.Clear();
            GameplayMode mode = UI.FilterUI.Mode;
            if (!mode.IsSolo() || GameDataModel.instance == null)
                return;

            var profile = GameDataModel.instance.gameDynamicData.GetCurrentPlayerDynamicData();
            if (profile == null) return;

            // The native per-level getter would create a save entry for every unplayed chart.
            foreach (var entry in Stats(profile))
                if (entry.gameplayMode == mode && entry.validScore)
                    PlayedSoloLevels.Add(entry.levelID);
        }

        public static bool HasLocalScore(BeatmapLevel level)
        {
            GameplayMode mode = UI.FilterUI.Mode;
            if (mode.IsSolo())
                return PlayedSoloLevels.Contains(level.levelID);

            var scores = LocalLeaderboardsModel.instance;
            if (scores == null || level.difficultyBeatmaps == null)
                return false;
            return level.difficultyBeatmaps.Any(difficulty =>
                scores.GetScores(LeaderboardsModel.GetLeaderboardID(difficulty, mode),
                    LocalLeaderboardsModel.LeaderboardType.AllTime)?.Count > 0);
        }
    }
}
