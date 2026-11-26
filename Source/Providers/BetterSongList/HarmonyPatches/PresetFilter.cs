#if !LEGATO_BEFORE_MULTIPLAYER
using HarmonyLib;

namespace BetterSongList.HarmonyPatches {
	#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(LevelSearchViewController), "ResetCurrentFilterParams")]
#else
    [HarmonyPatch(typeof(LevelSearchViewController), nameof(LevelSearchViewController.ResetOptionFilterSettings))]
#endif
	static class PresetFilter {
		static void Postfix(LevelSearchViewController __instance) {
            if(Config.Instance.AutoFilterUnowned) {
#if BEAT_SABER_1_29_0
                __instance._currentFilterParams.filterByOwned = true;
#else
                __instance._currentSearchFilter.songOwned = true;
#endif
            }
		}
	}
}
#endif
