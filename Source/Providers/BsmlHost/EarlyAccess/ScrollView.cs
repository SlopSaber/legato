#if LEGATO_EARLY_VR_UI
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HMUI;

public class ItemForFocussedScrolling : MonoBehaviour
{
}

public class VerticalScrollIndicator : MonoBehaviour
{
    protected RectTransform _handle;
    private float pageHeight = 1f;
    private float scrollProgress;

    public float normalizedPageHeight
    {
        get => pageHeight;
        set
        {
            float next = Mathf.Clamp01(value);
            if (!Mathf.Approximately(next, pageHeight))
            {
                pageHeight = next;
                RefreshHandle();
            }
        }
    }

    public float progress
    {
        get => scrollProgress;
        set
        {
            float next = Mathf.Clamp01(value);
            if (!Mathf.Approximately(next, scrollProgress))
            {
                scrollProgress = next;
                RefreshHandle();
            }
        }
    }

    private void RefreshHandle()
    {
        if (_handle == null)
        {
            return;
        }

        float bottom = (1f - scrollProgress) * (1f - pageHeight);
        _handle.anchorMin = new Vector2(0f, bottom);
        _handle.anchorMax = new Vector2(1f, bottom + pageHeight);
        _handle.offsetMin = _handle.offsetMax = Vector2.zero;
    }
}

public class ScrollView : MonoBehaviour, IBeginDragHandler
{
    internal RectTransform Viewport { get; private set; }
    internal RectTransform Content { get; private set; }
    internal VerticalScrollIndicator Indicator { get; set; }

    private ScrollRect scrollRect;
    private Button pageUp;
    private Button pageDown;
    private bool moving;
    private float destination;
    private float previousContentHeight = -1f;
    private float previousViewportHeight = -1f;

    public float position => Mathf.Clamp(Content.anchoredPosition.y, 0f, Travel);

    protected float Travel => Mathf.Max(0f, Content.rect.height - Viewport.rect.height);

    internal void Bind(ScrollRect rect, RectTransform viewport, RectTransform content)
    {
        scrollRect = rect;
        Viewport = viewport;
        Content = content;
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    internal void BindPaging(Button up, Button down)
    {
        if (pageUp != null)
        {
            pageUp.onClick.RemoveListener(PageUpButtonPressed);
        }

        if (pageDown != null)
        {
            pageDown.onClick.RemoveListener(PageDownButtonPressed);
        }

        pageUp = up;
        pageDown = down;
        if (pageUp != null)
        {
            pageUp.onClick.AddListener(PageUpButtonPressed);
        }

        if (pageDown != null)
        {
            pageDown.onClick.AddListener(PageDownButtonPressed);
        }
    }

    public void PageUpButtonPressed() => Page(-1);

    public void PageDownButtonPressed() => Page(1);

    public void OnBeginDrag(PointerEventData eventData) => moving = false;

    public void ScrollDown(bool animated) => ScrollTo(Travel, animated);

    public void ScrollTo(float offset, bool animated)
    {
        destination = Mathf.Clamp(offset, 0f, Travel);
        scrollRect.StopMovement();
        moving = animated && !Mathf.Approximately(position, destination);
        if (!moving)
        {
            SetPosition(destination);
        }

        RefreshButtons();
    }

    public void ScrollToWorldPosition(Vector3 worldPosition, float pageRelativePosition, bool animated)
    {
        float contentOffset = -Content.InverseTransformPoint(worldPosition).y;
        ScrollTo(contentOffset - (pageRelativePosition * Viewport.rect.height), animated);
    }

    public void ScrollToWorldPositionIfOutsideArea(Vector3 worldPosition, float pageRelativePosition,
        float relativeBoundaryStart, float relativeBoundaryEnd, bool animated)
    {
        float contentOffset = -Content.InverseTransformPoint(worldPosition).y - position;
        float height = Viewport.rect.height;
        if (contentOffset < relativeBoundaryStart * height || contentOffset > relativeBoundaryEnd * height)
        {
            ScrollToWorldPosition(worldPosition, pageRelativePosition, animated);
        }
    }

    public void SetContentSize(float height)
    {
        Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, height));
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        float travel = Travel;
        if (pageUp != null)
        {
            pageUp.interactable = position > 0.01f;
        }

        if (pageDown != null)
        {
            pageDown.interactable = position < travel - 0.01f;
        }

        if (Indicator != null)
        {
            Indicator.normalizedPageHeight = Content.rect.height > 0f ? Viewport.rect.height / Content.rect.height : 1f;
            Indicator.progress = travel > 0f ? position / travel : 0f;
        }
    }

    protected virtual void OnScrollLayoutChanged()
    {
        SetPosition(Mathf.Clamp(Content.anchoredPosition.y, 0f, Travel));
    }

    protected virtual void LateUpdate()
    {
        if (Content == null || Viewport == null)
        {
            return;
        }

        if (!Mathf.Approximately(previousContentHeight, Content.rect.height) ||
            !Mathf.Approximately(previousViewportHeight, Viewport.rect.height))
        {
            previousContentHeight = Content.rect.height;
            previousViewportHeight = Viewport.rect.height;
            OnScrollLayoutChanged();
        }

        if (moving)
        {
            destination = Mathf.Clamp(destination, 0f, Travel);
            float next = Mathf.Lerp(position, destination, Time.unscaledDeltaTime * 12f);
            moving = Mathf.Abs(next - destination) > 0.05f;
            SetPosition(moving ? next : destination);
        }

        RefreshButtons();
    }

    protected void SetPosition(float value)
    {
        var anchored = Content.anchoredPosition;
        anchored.y = value;
        Content.anchoredPosition = anchored;
    }

    private void Page(int direction)
    {
        float origin = moving ? destination : position;
        float target = origin + (direction * Viewport.rect.height * 0.9f);
        foreach (var item in Content.GetComponentsInChildren<ItemForFocussedScrolling>())
        {
            float offset = -Content.InverseTransformPoint(item.transform.position).y;
            if ((direction > 0 && offset > origin + 0.1f && offset < target) ||
                (direction < 0 && offset < origin - 0.1f && offset > target))
            {
                target = offset;
            }
        }

        ScrollTo(target, true);
    }
}

public sealed class TextPageScrollView : ScrollView
{
    internal TextMeshProUGUI _text;
    private string measuredText;

    public void SetText(string text)
    {
        _text.text = text ?? string.Empty;
        float width = Viewport.rect.width;
        if (width > 0f)
        {
            SetContentSize(_text.GetPreferredValues(_text.text, width, 0f).y);
            measuredText = _text.text;
        }
    }

    protected override void OnScrollLayoutChanged()
    {
        SetText(_text.text);
        base.OnScrollLayoutChanged();
    }

    protected override void LateUpdate()
    {
        if (_text != null && _text.text != measuredText)
        {
            SetText(_text.text);
        }

        base.LateUpdate();
    }
}
#endif
