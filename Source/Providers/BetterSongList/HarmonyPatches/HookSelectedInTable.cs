using HarmonyLib;

namespace BetterSongList.HarmonyPatches {
	[HarmonyPatch(typeof(LevelCollectionTableView), Legato.UI.CollectionNavigation.RowSelectionMethod)]
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
