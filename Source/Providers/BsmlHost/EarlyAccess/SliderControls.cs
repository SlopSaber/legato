#if LEGATO_EARLY_VR_UI
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HMUI;

public class TextSlider : Slider
{
    private int steps;
    private Func<float, string> formatter;
    private Button decrement;
    private Button increment;

    public event Action<TextSlider, float> valueDidChangeEvent;

    public int numberOfSteps
    {
        get => steps;
        set
        {
            steps = Mathf.Max(0, value);
            Set(base.value, false);
        }
    }

    internal TextMeshProUGUI ValueLabel { get; private set; }

    internal void Bind(TextMeshProUGUI valueLabel, Button decrease, Button increase)
    {
        ValueLabel = valueLabel;
        decrement = decrease;
        increment = increase;
        decrement.onClick.AddListener(() => MoveStep(-1));
        increment.onClick.AddListener(() => MoveStep(1));
        Refresh();
    }

    internal void SetFormatter(Func<float, string> format)
    {
        formatter = format;
        Refresh();
    }

    internal void SetWithoutNotification(float value) => Set(value, false);

    internal void SetStepButtons(bool show)
    {
        decrement.gameObject.SetActive(show);
        increment.gameObject.SetActive(show);
        float spacing = show ? 6f : 0f;
        var rect = (RectTransform)transform;
        rect.offsetMin = new Vector2(spacing, 0f);
        rect.offsetMax = new Vector2(-spacing, 0f);
        Refresh();
    }

    public void Refresh()
    {
        if (ValueLabel != null)
        {
            ValueLabel.text = formatter != null ? formatter(base.value) : base.value.ToString("N2");
            decrement.interactable = interactable && base.value > minValue;
            increment.interactable = interactable && base.value < maxValue;
        }
    }

    protected override void Set(float input, bool sendCallback)
    {
        float previousValue = base.value;
        if (steps > 1 && maxValue > minValue)
        {
            float fraction = Mathf.InverseLerp(minValue, maxValue, input);
            float step = Mathf.Round(fraction * (steps - 1));
            input = Mathf.Lerp(minValue, maxValue, step / (steps - 1));
        }

        base.Set(input, sendCallback);
        Refresh();
        if (sendCallback && previousValue != base.value)
        {
            valueDidChangeEvent?.Invoke(this, base.value);
        }
    }

    private void MoveStep(int direction)
    {
        float delta = (maxValue - minValue) / (steps > 1 ? steps - 1 : 10);
        value += direction * delta;
    }
}

public sealed class RangeValuesTextSlider : TextSlider
{
}
#endif
