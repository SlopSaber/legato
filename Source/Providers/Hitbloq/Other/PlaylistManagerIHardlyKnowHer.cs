using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities;
using IPA.Utilities.Async;
using PlaylistManager;
using PlaylistManager.Utilities;
using SiraUtil.Web;
using Zenject;
using LibraryPlaylist = BeatSaberPlaylistsLib.Types.IPlaylist;
using Utils = Hitbloq.Utilities.Utils;

namespace Hitbloq.Other
{
	internal class PlaylistManagerIHardlyKnowHer : IInitializable, IDisposable
	{
		private readonly LevelFilteringNavigationController _levelFilteringNavigationController;
		private readonly MainFlowCoordinator _mainFlowCoordinator;
#if !LEGATO_BEFORE_MULTIPLAYER
		private readonly IconSegmentedControl _levelCategorySegmentedControl;
		private readonly MainMenuViewController _mainMenuViewController;
		private readonly SelectLevelCategoryViewController _selectLevelCategoryViewController;
#endif
		private readonly IHttpService _siraHttpService;
		private readonly SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator;
		private readonly PlaylistDataManager _playlistDataManager;

		private CancellationTokenSource? _tokenSource;

		public PlaylistManagerIHardlyKnowHer(IHttpService siraHttpService, MainFlowCoordinator mainFlowCoordinator, SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator, LevelFilteringNavigationController levelFilteringNavigationController, PlaylistDataManager playlistDataManager
#if !LEGATO_BEFORE_MULTIPLAYER
			, MainMenuViewController mainMenuViewController, SelectLevelCategoryViewController selectLevelCategoryViewController
#endif
			)
		{
			_siraHttpService = siraHttpService;
			_mainFlowCoordinator = mainFlowCoordinator;
			_soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
			_levelFilteringNavigationController = levelFilteringNavigationController;
			_playlistDataManager = playlistDataManager;
#if !LEGATO_BEFORE_MULTIPLAYER
			_selectLevelCategoryViewController = selectLevelCategoryViewController;
			_mainMenuViewController = mainMenuViewController;
			_levelCategorySegmentedControl = selectLevelCategoryViewController.GetField<IconSegmentedControl, SelectLevelCategoryViewController>("_levelFilterCategoryIconSegmentedControl");
#endif
		}

		public LibraryPlaylist? SelectedPlaylist =>
			_playlistDataManager.selectedPlaylist;


		public bool IsDownloading => _tokenSource is {IsCancellationRequested: false};

		public bool CanOpenPlaylist
		{
			get
			{
				var currentFlow = _mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
				var parentFlow = Accessors.ParentFlowAccessor(ref currentFlow);
				return parentFlow is LevelSelectionFlowCoordinator or MainFlowCoordinator;
			}
		}

		public void Dispose()
		{
			Events.playlistSelected -= OnPlaylistSelected;
			_tokenSource?.Cancel();
			_tokenSource?.Dispose();
			_tokenSource = null;
		}

		public void Initialize()
		{
			Events.playlistSelected += OnPlaylistSelected;
		}

		public event Action<string>? HitbloqPlaylistSelected;

		public void DownloadOrOpenPlaylist(string poolID, Action? onDownloadComplete = null)
		{
			_ = DownloadOrOpenPlaylistAsync(poolID, onDownloadComplete);
		}

		private async Task DownloadOrOpenPlaylistAsync(string poolID, Action? onDownloadComplete = null)
		{
			_tokenSource?.Cancel();
			_tokenSource?.Dispose();
			var tokenSource = new CancellationTokenSource();
			_tokenSource = tokenSource;
			try
			{
				var playlistToSelect = await GetPlaylist(poolID, tokenSource.Token);
				if (playlistToSelect == null || tokenSource.IsCancellationRequested)
				{
					return;
				}

				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					if (tokenSource.IsCancellationRequested) return;
					onDownloadComplete?.Invoke();
					OpenPlaylist(playlistToSelect);
				});
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				if (ReferenceEquals(_tokenSource, tokenSource)) _tokenSource = null;
				tokenSource.Dispose();
			}
		}

		public void CancelDownload()
		{
			_tokenSource?.Cancel();
		}

		private async Task<HitbloqNativePack?> GetPlaylist(string poolID, CancellationToken token = default)
		{
			var localPlaylist = await FindLocalPlaylistFromPoolID(poolID, token);
			if (localPlaylist != null)
			{
				return localPlaylist;
			}

			token.ThrowIfCancellationRequested();
			return await DownloadPlaylistFromPoolID(poolID, token);
		}

		public async Task<HitbloqNativePack?> FindLocalPlaylistFromPoolID(string poolID, CancellationToken token = default)
		{
			try
			{
				var playlist = await Task.Run(() =>
				{
					var syncURL = $"https://hitbloq.com/static/hashlists/{poolID}.bplist";

					foreach (var playlist in BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true))
					{
						token.ThrowIfCancellationRequested();
						if (playlist.TryGetCustomData("syncURL", out var url) && url is string urlString)
						{
							if (urlString == syncURL)
							{
								return playlist;
							}
						}
					}

					return null;
				}, token);
				return playlist?.PlaylistLevelPack;

			}
			catch (OperationCanceledException)
			{
				return null;
			}
		}

		public async Task<HitbloqNativePack?> DownloadPlaylistFromPoolID(string poolID, CancellationToken token = default)
		{
			try
			{
				var syncURL = $"https://hitbloq.com/static/hashlists/{poolID}.bplist";
				var webResponse = await _siraHttpService.GetAsync(syncURL, cancellationToken: token);
				var playlistBytes = await webResponse.ReadAsByteArrayAsync();
				token.ThrowIfCancellationRequested();
				using Stream playlistStream = new MemoryStream(playlistBytes);
				var newPlaylist = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.DefaultHandler?.Deserialize(playlistStream);

				token.ThrowIfCancellationRequested();
				if (newPlaylist != null)
				{
					var playlistManager = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager("Hitbloq");
					playlistManager.StorePlaylist(newPlaylist);
				}

				return newPlaylist?.PlaylistLevelPack;

			}
			catch (OperationCanceledException)
			{
				return null;
			}
		}

		public void OpenPlaylist(HitbloqNativePack beatmapLevelPack)
		{
#if LEGATO_BEFORE_MULTIPLAYER
			var current = _mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
			if (current is LevelSelectionFlowCoordinator)
			{
				SelectLegacyPack(beatmapLevelPack);
			}
			else if (current == _mainFlowCoordinator)
			{
				_soloFreePlayFlowCoordinator.Setup(null, null);
				_mainFlowCoordinator.PresentFlowCoordinator(_soloFreePlayFlowCoordinator, () => SelectLegacyPack(beatmapLevelPack));
			}
#else
			if (_mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf() is LevelSelectionFlowCoordinator)
			{
				_levelCategorySegmentedControl.SelectCellWithNumber(1);
				_selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(_levelCategorySegmentedControl, 1);
				_levelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(beatmapLevelPack);
			}
			else
			{
				_soloFreePlayFlowCoordinator.Setup(Utils.GetStateForPlaylist(beatmapLevelPack));
				_mainMenuViewController.HandleMenuButton(MainMenuViewController.MenuButton.SoloFreePlay);
			}
#endif
		}

#if LEGATO_BEFORE_MULTIPLAYER
		private void SelectLegacyPack(IBeatmapLevelPack beatmapLevelPack)
		{
			var tab = _levelFilteringNavigationController._customLevelsTabBarData;
			if (tab == null)
				return;

			var packs = tab.annotatedBeatmapLevelCollections ?? Array.Empty<IAnnotatedBeatmapLevelCollection>();
			if (!packs.OfType<IBeatmapLevelPack>().Any(pack => pack.packID == beatmapLevelPack.packID))
			{
				tab.annotatedBeatmapLevelCollections = packs.Append(beatmapLevelPack).ToArray();
			}
			_levelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(beatmapLevelPack);
		}
#endif

		private void OnPlaylistSelected(LibraryPlaylist playlist, BeatSaberPlaylistsLib.PlaylistManager parentManager)
		{
            var pool = GetPlaylistPool(playlist);
			if (pool is not null)
			{
				HitbloqPlaylistSelected?.Invoke(pool!);
			}
		}

		public static string? GetPlaylistPool(LibraryPlaylist playlist)
		{
			if (!playlist.TryGetCustomData("syncURL", out var url) || url is not string urlString)
			{
				return null;
			}

			if (!urlString.Contains("https://hitbloq.com/static/hashlists/"))
			{
				return null;
			}

			var pool = urlString.Split('/').LastOrDefault()?.Split('.').FirstOrDefault();
			return !string.IsNullOrEmpty(pool) ? pool : null;
		}
	}
}
