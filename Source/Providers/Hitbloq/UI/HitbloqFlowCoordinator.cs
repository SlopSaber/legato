using BeatSaberMarkupLanguage;
using Hitbloq.Entries;
using Hitbloq.UI.ViewControllers;
using HMUI;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
	internal class HitbloqFlowCoordinator : FlowCoordinator
	{
		[Inject]
		private readonly HitbloqInfoViewController _hitbloqInfoViewController = null!;

		[Inject]
		private readonly HitbloqNavigationController _hitbloqNavigationController = null!;

		[Inject]
		private readonly HitbloqPoolDetailViewController _hitbloqPoolDetailViewController = null!;

		[Inject]
		private readonly HitbloqPoolLeaderboardViewController _hitbloqPoolLeaderboardViewController = null!;

		[Inject]
		private readonly HitbloqPoolListViewController _hitbloqPoolListViewController = null!;

		[Inject]
		private readonly HitbloqRankedListViewController _hitbloqRankedListViewController = null!;

		[Inject]
		private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

		[Inject]
		private readonly PopupModalsController _popupModalsController = null!;

		private HitbloqPoolListEntry? _currentPool;

		private bool _flowAnimationComplete;

		private FlowCoordinator? _presentingFlowCoordinator;

#if LEGATO_LEGACY_FLOW_OVERRIDE
		public
#else
		protected
#endif
#if LEGATO_BEFORE_MULTIPLAYER
		override void DidActivate(bool firstActivation, ActivationType activationType)
#else
		override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
#endif
		{
#if LEGATO_BEFORE_MULTIPLAYER
			if (activationType != ActivationType.AddedToHierarchy)
				return;
			title = "Hitbloq";
#else
			SetTitle("Hitbloq");
#endif
			showBackButton = true;
			_flowAnimationComplete = false;
			_currentPool = null;

			SetViewControllersToNavigationController(_hitbloqNavigationController, _hitbloqPoolListViewController);
			ProvideInitialViewControllers(_hitbloqNavigationController, bottomScreenViewController: _hitbloqInfoViewController);

			_hitbloqPoolListViewController.PoolSelectedEvent += OnPoolSelected;
			_hitbloqPoolListViewController.DetailDismissRequested += OnDetailDismissRequested;
			_hitbloqPoolDetailViewController.FlowDismissRequested += OnFlowDismissRequested;
			_hitbloqInfoViewController.URLOpenRequested += OnURLOpenRequested;
		}

#if LEGATO_LEGACY_FLOW_OVERRIDE
		public
#else
		protected
#endif
#if LEGATO_BEFORE_MULTIPLAYER
		override void DidDeactivate(DeactivationType deactivationType)
#else
		override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
#endif
		{
#if LEGATO_BEFORE_MULTIPLAYER
			base.DidDeactivate(deactivationType);
			if (deactivationType != DeactivationType.RemovedFromHierarchy)
				return;
#else
			base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
#endif
			_hitbloqPoolListViewController.PoolSelectedEvent -= OnPoolSelected;
			_hitbloqPoolListViewController.DetailDismissRequested -= OnDetailDismissRequested;
			_hitbloqPoolDetailViewController.FlowDismissRequested -= OnFlowDismissRequested;
			_hitbloqInfoViewController.URLOpenRequested -= OnURLOpenRequested;
		}

		private void OnPoolSelected(HitbloqPoolListEntry pool)
		{
			if (_flowAnimationComplete)
			{
				if (!_hitbloqPoolDetailViewController.isInViewControllerHierarchy)
				{
					PushViewControllerToNavigationController(_hitbloqNavigationController, _hitbloqPoolDetailViewController);
				}

#if LEGATO_BEFORE_MULTIPLAYER
				SetLeftScreenViewController(_hitbloqRankedListViewController, false);
				SetRightScreenViewController(_hitbloqPoolLeaderboardViewController, false);
#else
				SetLeftScreenViewController(_hitbloqRankedListViewController, ViewController.AnimationType.In);
				SetRightScreenViewController(_hitbloqPoolLeaderboardViewController, ViewController.AnimationType.In);
#endif

				_hitbloqPoolDetailViewController.SetPool(pool);
				_hitbloqRankedListViewController.SetPool(pool.ID);
				_hitbloqPoolLeaderboardViewController.SetPool(pool.ID);
			}

			_currentPool = pool;
		}

		private void OnDetailDismissRequested()
		{
			if (_hitbloqPoolDetailViewController.isInViewControllerHierarchy)
			{
				PopViewControllerFromNavigationController(_hitbloqNavigationController, immediately: true);
			}

			if (_hitbloqRankedListViewController.isInViewControllerHierarchy)
			{
#if LEGATO_BEFORE_MULTIPLAYER
				SetLeftScreenViewController(null, false);
#else
				SetLeftScreenViewController(null, ViewController.AnimationType.Out);
#endif
			}

			if (_hitbloqPoolLeaderboardViewController.isInViewControllerHierarchy)
			{
#if LEGATO_BEFORE_MULTIPLAYER
				SetRightScreenViewController(null, false);
#else
				SetRightScreenViewController(null, ViewController.AnimationType.Out);
#endif
			}
		}

		private void OnFlowDismissRequested()
		{
			_presentingFlowCoordinator?.DismissFlowCoordinator(this, immediately: true);
		}

		private void OnURLOpenRequested(string url)
		{
			_popupModalsController.ShowYesNoModal(_hitbloqPoolListViewController.rectTransform, $"Would you like to open\n{url}", () => { Application.OpenURL(url); });
		}

#if LEGATO_LEGACY_FLOW_OVERRIDE
		public
#else
		protected
#endif
		override void BackButtonWasPressed(ViewController topViewController)
		{
			_hitbloqRankedListViewController.gameObject.SetActive(false);
			_hitbloqPoolLeaderboardViewController.gameObject.SetActive(false);
			_hitbloqInfoViewController.gameObject.SetActive(false);
			_presentingFlowCoordinator?.DismissFlowCoordinator(this);
		}

		internal void Show()
		{
			_presentingFlowCoordinator = _mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
			_presentingFlowCoordinator.PresentFlowCoordinator(this, () =>
			{
				_flowAnimationComplete = true;
				if (_currentPool != null)
				{
					OnPoolSelected(_currentPool);
				}
			});
		}

		internal void ShowAndOpenPoolWithID(string? poolID)
		{
			_hitbloqPoolListViewController.poolToOpen = poolID;
			Show();
		}
	}

	internal class HitbloqNavigationController : NavigationController
	{
	}
}
