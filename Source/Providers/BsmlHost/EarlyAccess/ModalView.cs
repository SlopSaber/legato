#if LEGATO_EARLY_VR_UI
using System;
using System.Collections;
using IPA.Utilities.Async;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using VRUIControls;

namespace HMUI;

public sealed class ModalView : MonoBehaviour
{
    private RectTransform overlay;
    private RectTransform originalParent;
    private int originalSibling;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalSizeDelta;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private CanvasGroup canvasGroup;
    private Coroutine transition;
    private int presentation;

    public event Action blockerClickedEvent;

    public void Show(bool animate, bool moveToCenter = true)
    {
        if (overlay != null)
        {
            presentation++;
            ChangeAlpha(1f, animate, null);
            return;
        }

        RectTransform panel = (RectTransform)transform;
        originalParent = (RectTransform)panel.parent;
        originalSibling = panel.GetSiblingIndex();
        originalAnchorMin = panel.anchorMin;
        originalAnchorMax = panel.anchorMax;
        originalSizeDelta = panel.sizeDelta;
        originalPosition = panel.anchoredPosition3D;
        originalRotation = panel.localRotation;
        originalScale = panel.localScale;

        VRUIViewController owner = GetComponentInParent<VRUIViewController>();
        Canvas parentCanvas = owner != null ? owner.GetComponentInParent<Canvas>() : originalParent.GetComponentInParent<Canvas>();
        Transform root = owner != null ? owner.rectTransform : parentCanvas != null ? parentCanvas.transform : originalParent;
        overlay = CreateStretchRect(root, "Modal Overlay");
        Canvas canvas = overlay.gameObject.AddComponent<Canvas>();
        if (parentCanvas != null)
        {
            canvas.worldCamera = parentCanvas.worldCamera;
            canvas.sortingLayerID = parentCanvas.sortingLayerID;
        }

        int order = 0;
        foreach (Canvas siblingCanvas in root.GetComponentsInChildren<Canvas>())
        {
            if (siblingCanvas != canvas && siblingCanvas.sortingLayerID == canvas.sortingLayerID)
            {
                order = Mathf.Max(order, siblingCanvas.sortingOrder);
            }
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = order + 1;
        overlay.gameObject.AddComponent<VRGraphicRaycaster>();
        canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = animate ? 0f : 1f;

        RectTransform blocker = CreateStretchRect(overlay, "Modal Blocker");
        UnityEngine.UI.Image blockerImage = blocker.gameObject.AddComponent<UnityEngine.UI.Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.55f);
        Button button = blocker.gameObject.AddComponent<Button>();
        button.targetGraphic = blockerImage;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => blockerClickedEvent?.Invoke());

        Vector2 size = panel.rect.size;
        Vector3 position = panel.position;
        panel.SetParent(overlay, true);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = size;
        if (moveToCenter)
        {
            panel.anchoredPosition3D = Vector3.zero;
            panel.localRotation = Quaternion.identity;
        }
        else
        {
            panel.position = position;
        }

        presentation++;
        gameObject.SetActive(true);
        ChangeAlpha(1f, animate, null);
    }

    public void Hide(bool animate, Action finishedCallback = null)
    {
        if (overlay == null)
        {
            finishedCallback?.Invoke();
            return;
        }

        ChangeAlpha(0f, animate, () =>
        {
            RectTransform previousOverlay = overlay;
            overlay = null;
            gameObject.SetActive(false);
            bool canRestore = originalParent != null;
            if (canRestore)
            {
                RectTransform panel = (RectTransform)transform;
                panel.SetParent(originalParent, false);
                panel.SetSiblingIndex(originalSibling);
                panel.anchorMin = originalAnchorMin;
                panel.anchorMax = originalAnchorMax;
                panel.sizeDelta = originalSizeDelta;
                panel.anchoredPosition3D = originalPosition;
                panel.localRotation = originalRotation;
                panel.localScale = originalScale;
            }

            previousOverlay.gameObject.SetActive(false);
            Destroy(previousOverlay.gameObject);
            if (canRestore)
            {
                finishedCallback?.Invoke();
            }
        });
    }

    private static RectTransform CreateStretchRect(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private void ChangeAlpha(float target, bool animate, Action completion)
    {
        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }

        if (animate && isActiveAndEnabled && !Mathf.Approximately(canvasGroup.alpha, target))
        {
            transition = StartCoroutine(Fade(target, completion));
        }
        else
        {
            canvasGroup.alpha = target;
            completion?.Invoke();
        }
    }

    private IEnumerator Fade(float target, Action completion)
    {
        const float duration = 0.12f;
        float start = canvasGroup.alpha;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = target;
        transition = null;
        completion?.Invoke();
    }

    private void OnDisable()
    {
        if (overlay == null)
        {
            return;
        }

        int closedPresentation = presentation;
        _ = UnityMainThreadTaskScheduler.Factory.StartNew(() =>
        {
            if (this != null && overlay != null && presentation == closedPresentation)
            {
                Hide(false);
            }
        });
    }

    private void OnDestroy()
    {
        if (overlay != null)
        {
            Destroy(overlay.gameObject);
        }
    }
}
#endif
