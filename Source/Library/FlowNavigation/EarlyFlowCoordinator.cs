#if LEGATO_EARLY_STANDARD_MAPS
#nullable enable
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace Legato.Presentation {
    internal abstract class EarlyFlowCoordinator : VRUINavigationController {
        private const float HeaderHeight = 8f;

        private VRUIViewController? _main;
        private VRUIViewController? _left;
        private VRUIViewController? _right;
        private bool _hasPending;
        private VRUIViewController? _pendingMain;
        private VRUIViewController? _pendingLeft;
        private VRUIViewController? _pendingRight;
        private RectTransform? _header;
        private TextMeshProUGUI? _titleText;
        private Button? _backButton;
        private bool _presenting;
        private string _title = string.Empty;

        protected string title {
            get => _title;
            set {
                _title = value ?? string.Empty;
                if (_titleText != null) _titleText.text = _title;
            }
        }

        protected bool showBackButton { get; set; }

        internal VRUIViewController? topViewController => _main;

        protected void SetTitle(string value) => title = value;

        protected virtual void Awake() {
            gameObject.layer = 5;
            if (!(transform is RectTransform)) gameObject.AddComponent<RectTransform>();
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            if (GetComponent<Canvas>() == null) {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            }
            if (GetComponent<VRUIControls.VRGraphicRaycaster>() == null) gameObject.AddComponent<VRUIControls.VRGraphicRaycaster>();
        }

        protected void ProvideInitialViewControllers(VRUIViewController mainViewController,
            VRUIViewController? leftViewController = null, VRUIViewController? rightViewController = null) {
            _pendingMain = mainViewController;
            _pendingLeft = leftViewController;
            _pendingRight = rightViewController;
            _hasPending = true;
        }

        protected void SetLeftScreenViewController(VRUIViewController? viewController) =>
            SetSideView(screen?.screenSystem?.leftScreen, ref _left, viewController);

        protected void SetRightScreenViewController(VRUIViewController? viewController) =>
            SetSideView(screen?.screenSystem?.rightScreen, ref _right, viewController);

        protected virtual void BackButtonWasPressed(VRUIViewController? topViewController) => Dismiss();

        internal bool Present() {
            if (_presenting || isInViewControllerHierarchy) return false;
            VRUIViewController? parent = UnityEngine.Resources.FindObjectsOfTypeAll<MainMenuViewController>()
                .FirstOrDefault(menu => menu.gameObject.scene.IsValid() && menu.isInViewControllerHierarchy);
            if (parent == null) return false;
            while (parent.childViewController != null) parent = parent.childViewController;
            if (!parent.isActiveAndEnabled) return false;
            _presenting = true;
            parent.PresentModalViewController(this, () => _presenting = false, true);
            return true;
        }

        internal void Dismiss() {
            _hasPending = false;
            if (!isInViewControllerHierarchy) return;
            if (childViewController is EarlyFlowCoordinator child) child.Dismiss();
            if (childViewController == null) DismissModalViewController(null, true);
        }

        protected virtual void LateUpdate() {
            if (!isInViewControllerHierarchy) {
                if (!_presenting && _main != null) ClearViews();
                return;
            }
            if (_header == null) BuildHeader();
            if (_backButton != null && _backButton.gameObject.activeSelf != showBackButton)
                _backButton.gameObject.SetActive(showBackButton);
            if (_presenting || childViewController != null) return;

            if (_hasPending) {
                _hasPending = false;
                if (_main != _pendingMain) {
                    ClearChildControllers();
                    _main = _pendingMain;
                    if (_main != null) {
                        PushViewController(_main, true);
                        _main.rectTransform.offsetMax = new Vector2(_main.rectTransform.offsetMax.x, -HeaderHeight);
                    }
                }
                _header!.SetAsLastSibling();
                _left = _pendingLeft;
                _right = _pendingRight;
            }
            KeepSideView(screen?.screenSystem?.leftScreen, _left);
            KeepSideView(screen?.screenSystem?.rightScreen, _right);
        }

        private static void KeepSideView(VRUIScreen? target, VRUIViewController? view) {
            if (target != null && view != null && target.rootViewController != view) target.SetRootViewController(view);
        }

        private void ClearViews() {
            _main = null;
            _left = null;
            _right = null;
            _presenting = false;
        }

        private static void SetSideView(VRUIScreen? target, ref VRUIViewController? current, VRUIViewController? next) {
            if (current == next) return;
            current = next;
            if (target != null) target.SetRootViewController(next);
        }

        private void BuildHeader() {
            _header = CreateRect(transform, "Header");
            _header.anchorMin = new Vector2(0f, 1f);
            _header.anchorMax = Vector2.one;
            _header.pivot = new Vector2(0.5f, 1f);
            _header.sizeDelta = new Vector2(0f, HeaderHeight);
            _header.anchoredPosition = Vector2.zero;

            var titleRect = CreateRect(_header, "Title");
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(10f, 0f);
            titleRect.offsetMax = new Vector2(-10f, 0f);
            _titleText = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
            _titleText.font = TMP_Settings.defaultFontAsset;
            _titleText.fontSize = 4f;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.raycastTarget = false;
            _titleText.text = _title;

            var backRect = CreateRect(_header, "BackButton");
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.sizeDelta = new Vector2(8f, 6f);
            backRect.anchoredPosition = Vector2.zero;
            var background = backRect.gameObject.AddComponent<Image>();
            background.sprite = FindSprite("RoundRectButton");
            background.type = Image.Type.Sliced;
            _backButton = backRect.gameObject.AddComponent<Button>();
            _backButton.targetGraphic = background;
            _backButton.onClick.AddListener(() => BackButtonWasPressed(_main));

            var iconRect = CreateRect(backRect, "Icon");
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = new Vector2(-2f, -2f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = FindSprite("ArrowLeft");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            _backButton.gameObject.SetActive(showBackButton);
        }

        private static RectTransform CreateRect(Transform parent, string name) {
            var node = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)node.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Sprite? FindSprite(string name) =>
            UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(sprite => sprite.name == name);
    }
}
#endif
