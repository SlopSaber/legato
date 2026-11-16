using HarmonyLib;

namespace BetterSongList.HarmonyPatches {
	#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(LevelCollectionTableView), "HandleDidSelectRowEvent")]
#else
    [HarmonyPatch(typeof(LevelCollectionTableView), nameof(LevelCollectionTableView.HandleDidSelectCellWithIndex))]
#endif
	static class HookSelectedInTable {
		[HarmonyPriority(int.MinValue)]
		static void Postfix(LevelCollectionTableView __instance) {
			#if BEAT_SABER_1_29_0
            Config.Instance.LastSong = __instance._selectedPreviewBeatmapLevel?.levelID;
#else
            Config.Instance.LastSong = __instance._selectedBeatmapLevel?.levelID;
#endif

#if TRACE
			Plugin.Log.Debug(string.Format("LevelCollectionTableView.HandleDidSelectRowEvent(): LastSong: {0}", Config.Instance.LastSong));
#endif
		}
	}
}
