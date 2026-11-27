#if LEGATO_EARLY_VR_UI
using UnityEngine;
using UnityEngine.UI;

namespace HMUI;

public sealed class StackLayoutGroup : LayoutGroup
{
    private bool expandWidth = true;
    private bool expandHeight = true;

    public bool childForceExpandWidth
    {
        get => expandWidth;
        set => SetProperty(ref expandWidth, value);
    }

    public bool childForceExpandHeight
    {
        get => expandHeight;
        set => SetProperty(ref expandHeight, value);
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        Measure(0);
    }

    public override void CalculateLayoutInputVertical() => Measure(1);

    public override void SetLayoutHorizontal() => Arrange(0);

    public override void SetLayoutVertical() => Arrange(1);

    private void Measure(int axis)
    {
        int inset = axis == 0 ? padding.horizontal : padding.vertical;
        float minimum = 0f;
        float preferred = 0f;
        float flexible = 0f;
        foreach (RectTransform child in rectChildren)
        {
            minimum = Mathf.Max(minimum, inset + LayoutUtility.GetMinSize(child, axis));
            preferred = Mathf.Max(preferred, inset + LayoutUtility.GetPreferredSize(child, axis));
            flexible = Mathf.Max(flexible, Flex(child, axis));
        }

        SetLayoutInputForAxis(minimum, Mathf.Max(minimum, preferred), flexible, axis);
    }

    private void Arrange(int axis)
    {
        int inset = axis == 0 ? padding.horizontal : padding.vertical;
        float available = rectTransform.rect.size[axis] - inset;
        foreach (RectTransform child in rectChildren)
        {
            float minimum = LayoutUtility.GetMinSize(child, axis);
            float maximum = Flex(child, axis) > 0f ? available : LayoutUtility.GetPreferredSize(child, axis);
            float size = Mathf.Clamp(available, minimum, Mathf.Max(minimum, maximum));
            SetChildAlongAxis(child, axis, GetStartOffset(axis, size), size);
        }
    }

    private float Flex(RectTransform child, int axis)
    {
        bool expand = axis == 0 ? expandWidth : expandHeight;
        return Mathf.Max(LayoutUtility.GetFlexibleSize(child, axis), expand ? 1f : 0f);
    }
}
#endif
