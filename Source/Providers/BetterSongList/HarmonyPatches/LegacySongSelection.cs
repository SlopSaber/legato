#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Linq;
using BetterSongList.Interfaces;
using BetterSongList.UI;
using BetterSongList.Util;
using HarmonyLib;
using HMUI;
using Legato.SongBrowser;

namespace BetterSongList.HarmonyPatches {
    [HarmonyPatch(typeof(LevelFilteringNavigationController), "SendEventIfNeeded")]
    static class HookSelectedCollection {
        public static NativeSongPack lastSelectedCollection { get; private set; }

        [HarmonyPriority(Priority.Last)]
        static void Prefix(LevelFilteringNavigationController __instance, IAnnotatedBeatmapLevelCollection __0) {
            var pack = __0 as NativeSongPack;
            HookSelectedCategory.Set(__instance, __0);
            if (__0 == null)
                return;

            Config.Instance.LastPack = __0.collectionName ?? "";
            if (ReferenceEquals(pack, lastSelectedCollection)) {
                FilterUI.persistentNuts.UpdateTransformerOptionsAndDropdowns();
                return;
            }

            bool wasPlaylist = PlaylistsUtil.IsPlaylistPack(lastSelectedCollection);
            lastSelectedCollection = pack;
            FilterUI.persistentNuts.UpdateTransformerOptionsAndDropdowns();
            if (!Config.Instance.ClearFiltersOnPlaylistSelect)
                return;

            if (PlaylistsUtil.IsPlaylistPack(pack)) {
                FilterUI.SetSort(null, false, false);
                FilterUI.ClearFilter();
            } else if (wasPlaylist) {
                FilterUI.SetSort(Config.Instance.LastSort, false, false);
                FilterUI.SetFilter(Config.Instance.LastFilter, false, false);
            }
        }
    }

    static class HookSelectedCategory {
        public static LevelCategory lastSelectedCategory { get; private set; }

        internal static void Set(LevelFilteringNavigationController controller, IAnnotatedBeatmapLevelCollection collection) {
            var category = collection switch {
                UserFavoritesPlaylistSO => LevelCategory.Favorites,
                AllSongsPlaylistSO => LevelCategory.All,
                IPlaylist => LevelCategory.Playlists,
                _ => controller._tabBarViewController.selectedCellNumber switch {
                    0 => LevelCategory.OstAndExtras,
                    1 => LevelCategory.MusicPacks,
                    3 => LevelCategory.CustomSongs,
                    _ => LevelCategory.None
                }
            };
            if (collection != null)
                Config.Instance.LastCategory = category.ToString();
            if (lastSelectedCategory == category)
                return;

            lastSelectedCategory = category;
            RestoreTableScroll.ResetScroll();
        }
    }

    [HarmonyPatch(typeof(LevelSelectionFlowCoordinator), "DidActivate")]
    static class RestoreLegacyLevelSelection {
        static void Prefix(LevelSelectionFlowCoordinator __instance, FlowCoordinator.ActivationType activationType) {
            if (activationType != FlowCoordinator.ActivationType.AddedToHierarchy)
                return;

            if (!FilterUI.initialized)
                FilterUI.Init();

            if (__instance._annotatedBeatmapLevelCollectionToBeSelectedAfterPresent != null ||
                __instance._beatmapLevelToBeSelectedAfterPresent != null) {
                FilterUI.ClearSearch();
                FilterUI.ClearFilter();
                return;
            }

            var filtering = __instance._levelFilteringNavigationController;
            if (string.IsNullOrEmpty(Config.Instance.LastPack))
                return;

            var packName = Config.Instance.LastPack;
            IAnnotatedBeatmapLevelCollection collection = Config.Instance.LastCategory switch {
                nameof(LevelCategory.All) => filtering._allSongsPlaylist,
                nameof(LevelCategory.Favorites) => filtering._userFavoritesPlaylist,
                nameof(LevelCategory.Playlists) => filtering._filteredByBeatmapCharacteristicPlaylists
                    .FirstOrDefault(playlist => playlist.collectionName == packName),
                _ => PlaylistsUtil.GetPack(packName)
            };
            if (collection == null)
                return;

            var level = BrowserLevels.Find(filtering._beatmapLevelsModel, Config.Instance.LastSong);
            __instance.Setup(collection, level);
        }
    }

    [HarmonyPatch(typeof(LevelFilteringNavigationController), "InitializeTabBarViewController")]
    static class SeedLegacyCustomPacks {
        static void Postfix(LevelFilteringNavigationController __instance) {
            var customTab = __instance._customLevelsTabBarData;
            var packs = __instance._beatmapLevelsModel.customLevelPackCollection?.beatmapLevelPacks;
            if (customTab != null && packs != null)
                customTab.annotatedBeatmapLevelCollections = packs;
        }
    }
}
#endif
