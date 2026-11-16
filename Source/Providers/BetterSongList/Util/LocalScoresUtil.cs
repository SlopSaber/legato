using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace BetterSongList.Util {
	public static class LocalScoresUtil {
		public static PlayerDataModel playerDataModel { get; private set; }
		static HashSet<string> playedMaps = new HashSet<string>(500);

		public static bool hasScores => playerDataModel != null;

		public static void Load() {
			if (playerDataModel == null)
				playerDataModel = Legato.Unity.ObjectDiscovery.FindFirstActive<PlayerDataModel>();

            if(playerDataModel == null) return;
            foreach(var id in Legato.SongBrowser.BrowserLevels.PlayedIds(playerDataModel.playerData))
                playedMaps.Add(id);

		}

		[HarmonyPatch(typeof(PlayerLevelStatsData), nameof(PlayerLevelStatsData.UpdateScoreData))]
		static class InterceptNewScores {
			static void Prefix(PlayerLevelStatsData __instance) {
				// Will become valid after this UpdateScoreData() call
				if(!__instance._validScore)
					playedMaps.Add(__instance._levelID);
			}
		}

		public static bool HasLocalScore(string levelId) => playedMaps.Contains(levelId);

		public static bool HasLocalScore(BeatmapLevel level) => HasLocalScore(level.levelID);
	}
}
