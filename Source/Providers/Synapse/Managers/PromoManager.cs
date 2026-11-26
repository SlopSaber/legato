using System;
using HMUI;
using IPA.Utilities;
using JetBrains.Annotations;
using Synapse.Controllers;
using Synapse.Extras;
using Synapse.Networking.Models;
using Synapse.Views;
using TMPro;
#if LEGATO_BEFORE_MULTIPLAYER
using ImageView = BeatSaberMarkupLanguage.Components.LegacyImageView;
#else
using Tweening;
#endif
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace Synapse.Managers;

internal class PromoManager : IInitializable, ITickable, IDisposable
{
    private static readonly Sprite _promoBack =
        MediaExtensions.GetEmbeddedResourceSprite("Synapse.Resources.promo_back.png");

    private static readonly Sprite _promoPlaceholder =
        MediaExtensions.GetEmbeddedResourceSprite("Synapse.Resources.promo_placeholder.png");

#if !LEGATO_BEFORE_MULTIPLAYER
    private static readonly FieldAccessor<SelectableStateController, TimeTweeningManager>.Accessor
        _tweeningAccessor =
            FieldAccessor<SelectableStateController, TimeTweeningManager>.GetAccessor("_tweeningManager");
#endif

    private readonly IInstantiator _instantiator;
    private readonly EventModsViewController _eventModsViewController;

    private readonly ListingManager _listingManager;

    private readonly MainMenuViewController _mainMenuViewController;
#if LEGATO_BEFORE_MULTIPLAYER
    private CanvasGroup _bannerFade = null!;
#else
    private readonly TimeTweeningManager _tweeningManager;
    private GameObject _originalBanner = null!;
    private CanvasGroupTransitionSO _transition = null!;
#endif
    private ImageView _bannerTextBg = null!;

    private Button? _button;
    private ImageView _imageView = null!;

    private Listing? _listing;
    private GameObject _promoBanner = null!;

    private ImageViewRainbowController _rainbow = null!;
    private TextMeshProUGUI _textMesh = null!;

    private bool _finished;

    [UsedImplicitly]
    private PromoManager(
        MainMenuViewController mainMenuViewController,
        ListingManager listingManager,
#if !LEGATO_BEFORE_MULTIPLAYER
        TimeTweeningManager tweeningManager,
#endif
        IInstantiator instantiator,
        EventModsViewController eventModsViewController)
    {
        _instantiator = instantiator;
        _eventModsViewController = eventModsViewController;
        _mainMenuViewController = mainMenuViewController;
        _listingManager = listingManager;
#if !LEGATO_BEFORE_MULTIPLAYER
        _tweeningManager = tweeningManager;
#endif
        listingManager.ListingFound += OnListingFound;
        listingManager.BannerImageCreated += OnBannerImageCreated;
    }

    internal Button Button => _button ??= CreateButton();

    internal bool Active { get; private set; }

    public void Dispose()
    {
        _listingManager.ListingFound -= OnListingFound;
        _listingManager.BannerImageCreated -= OnBannerImageCreated;
#if LEGATO_BEFORE_MULTIPLAYER
        if (_promoBanner != null)
        {
            Object.Destroy(_promoBanner);
        }
#endif
    }

    public void Initialize()
    {
        RefreshListing(_listing);
    }

    public void Tick()
    {
        if (_finished || _listing == null)
        {
            return;
        }

        TimeSpan span = _listing.Time.ToTimeSpan();

        if (span.Ticks < 0)
        {
            Active = true;
            _finished = true;
            _rainbow.enabled = true;
            SetBannerAvailable(true);
            _textMesh.text = $"{_listing.Title}\n<size=120%>LIVE NOW</size>";
        }
        else if (span.TotalHours < 1 && _eventModsViewController.MissingMods is { Count: > 0 })
        {
            Active = true;
            SetBannerAvailable(true);
            _textMesh.text = $"{_listing.Title}\nREADY TO DOWNLOAD";
        }
        else
        {
            string time = span.Days switch
            {
                > 1 => $"{span.Days} days",
                > 0 => $"{span.Days} day",
                _ => $"{span.Hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}"
            };

            _textMesh.text = $"{_listing.Title}\n<size=120%>{time}</size>";
        }
    }

#if LEGATO_BEFORE_MULTIPLAYER
    private static T CreateBannerElement<T>(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        where T : Component
    {
        var element = new GameObject(name, typeof(RectTransform), typeof(T));
        element.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)element.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return element.GetComponent<T>();
    }
#endif

    private Button CreateButton()
    {
#if LEGATO_BEFORE_MULTIPLAYER
        Transform parent = _mainMenuViewController.transform;
        _promoBanner = new GameObject("SynapsePromoBanner", typeof(RectTransform), typeof(ImageView), typeof(Button), typeof(CanvasGroup));
        _promoBanner.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)_promoBanner.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -16);
        rect.sizeDelta = new Vector2(100, 21);

        ImageView frame = _promoBanner.GetComponent<ImageView>();
        frame.sprite = _promoBack;
        _bannerFade = _promoBanner.GetComponent<CanvasGroup>();
        Button button = _promoBanner.GetComponent<Button>();
        button.targetGraphic = frame;
        _rainbow = _instantiator.InstantiateComponent<ImageViewRainbowController>(_promoBanner);
        _rainbow.enabled = false;

        _imageView = CreateBannerElement<ImageView>(rect, "EventImage", new Vector2(0.02f, 0.08f), new Vector2(0.42f, 0.92f));
        _imageView.sprite = _promoPlaceholder;
        _imageView.preserveAspect = true;
        _imageView.raycastTarget = false;
        _bannerTextBg = CreateBannerElement<ImageView>(rect, "EventTextBackground", new Vector2(0.44f, 0.08f), new Vector2(0.98f, 0.92f));
        _bannerTextBg.color = Color.black;
        _bannerTextBg.raycastTarget = false;
        _textMesh = CreateBannerElement<TextMeshProUGUI>((RectTransform)_bannerTextBg.transform, "EventText", new Vector2(0.04f, 0), new Vector2(0.96f, 1));
        _textMesh.font = _mainMenuViewController._soloButton.GetComponentInChildren<TextMeshProUGUI>(true).font;
        _textMesh.alignment = TextAlignmentOptions.Center;
        _textMesh.enableAutoSizing = true;
        _textMesh.fontSizeMin = 2.5f;
        _textMesh.fontSizeMax = 5f;
        _textMesh.color = Color.white;
        _textMesh.raycastTarget = false;
        _promoBanner.SetActive(false);
        return button;
#else
        Button originalButton = _mainMenuViewController._musicPackPromoButton;
        GameObject original = originalButton.gameObject;
        Button newButton = Object.Instantiate(originalButton, original.transform.parent);
        GameObject newObject = newButton.gameObject;
        _promoBanner = newObject;
        _originalBanner = original;

        MusicPackPromoBanner musicPackPromoBanner = newObject.GetComponent<MusicPackPromoBanner>();
#if !LEGATO_DIFFICULTY_BEATMAPS
        Object.Destroy(musicPackPromoBanner._loadingIndicator);
        musicPackPromoBanner._promoBannerGo.SetActive(true);
        musicPackPromoBanner._backgroundImage.gameObject.SetActive(true);
#endif
        Object.Destroy(musicPackPromoBanner);

        SelectableStateController stateController =
            newObject.GetComponent<NoTransitionButtonSelectableStateController>();
        _tweeningAccessor(ref stateController) = _tweeningManager;

        newObject.transform.SetSiblingIndex(original.transform.GetSiblingIndex());

        _textMesh = newObject.GetComponentInChildren<TextMeshProUGUI>();
        _textMesh.text = string.Empty;

        _imageView = newObject.transform.Find("Banner").GetComponent<ImageView>();
        _imageView.sprite = _promoPlaceholder;
        GameObject originalImage = _imageView.gameObject;
        GameObject newImage = Object.Instantiate(originalImage, newObject.transform).gameObject;
        originalImage.transform.localScale = new Vector3(0.96f, 0.99f, 1);
        newImage.transform.SetSiblingIndex(0);
        newImage.GetComponent<ImageView>().sprite = _promoBack;
        _rainbow = _instantiator.InstantiateComponent<ImageViewRainbowController>(newImage);
        ////newButton.transform.localScale = new Vector3(1.00f, 1.0f, 1);

        // its on PromoText in 1.29 and on a child object in 1.34
        _bannerTextBg = newObject.transform.Find("PromoText").GetComponentInChildren<ImageView>(true);

        CanvasGroupStateTransition stateTransition = newObject.GetComponent<CanvasGroupStateTransition>();
        _transition = Object.Instantiate(stateTransition._transition);
        stateTransition._transition = _transition;

        newObject.name = "SynapsePromoBanner";

        _promoBanner = newObject;
        return newButton;
#endif
    }

    private void SetBannerAvailable(bool available)
    {
#if LEGATO_BEFORE_MULTIPLAYER
        _bannerFade.alpha = available ? 0.9f : 0.6f;
        Button.interactable = available;
#else
        _transition._normalAlpha = available ? 0.8f : 0.6f;
        _transition._highlightedAlpha = available ? 1f : 0.6f;
        Button.enabled = available;
        if (available)
        {
            NoTransitionButtonSelectableStateController controller =
                _promoBanner.GetComponent<NoTransitionButtonSelectableStateController>();
            controller.ResolveSelectionState(controller._component.selectionState, false);
        }
#endif
    }

    private void OnBannerImageCreated(Sprite? sprite)
    {
        _button ??= CreateButton();
        _imageView.sprite = sprite != null ? sprite : _promoPlaceholder;
    }

    private void OnListingFound(Listing? listing)
    {
        RefreshListing(listing);
    }

    private void RefreshListing(Listing? listing)
    {
        _button ??= CreateButton();
        _listing = listing;
        if (listing == null)
        {
            Active = false;
#if !LEGATO_BEFORE_MULTIPLAYER
            _originalBanner.SetActive(true);
#endif
            _promoBanner.SetActive(false);
        }
        else
        {
            _finished = false;
            Active = false;
            _rainbow.enabled = false;
            SetBannerAvailable(false);
#if !LEGATO_BEFORE_MULTIPLAYER
            _originalBanner.SetActive(false);
#endif
            _promoBanner.SetActive(true);
            if (ColorUtility.TryParseHtmlString(listing.BannerColor, out Color color))
            {
                _bannerTextBg.color = color;
            }
        }
    }
}
