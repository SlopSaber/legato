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
    private Color? textColor;

    public event Action<TextSlider, float> valueDidChangeEvent;

    public Color valueTextColor
    {
        get => textColor ?? (ValueLabel != null ? ValueLabel.color : Color.white);
        set
        {
            textColor = value;
            if (ValueLabel != null)
            {
                ValueLabel.color = value;
            }
        }
    }

    public int numberOfSteps
    {
        get => steps;
        set
        {
            steps = Mathf.Max(0, value);
            Set(SliderValue, false);
        }
    }

    internal TextMeshProUGUI ValueLabel { get; private set; }

    // Read the native slider value even if a derived control overrides its getter.
#pragma warning disable SA1100
    private float SliderValue => base.value;
#pragma warning restore SA1100

    public void Refresh()
    {
        if (ValueLabel != null)
        {
            ValueLabel.text = formatter != null ? formatter(SliderValue) : SliderValue.ToString("N2");
            decrement.interactable = interactable && SliderValue > minValue;
            increment.interactable = interactable && SliderValue < maxValue;
        }
    }

    internal void Bind(TextMeshProUGUI valueLabel, Button decrease, Button increase)
    {
        ValueLabel = valueLabel;
        if (textColor.HasValue)
        {
            ValueLabel.color = textColor.Value;
        }

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
        RectTransform rect = (RectTransform)transform;
        rect.offsetMin = new Vector2(spacing, 0f);
        rect.offsetMax = new Vector2(-spacing, 0f);
        Refresh();
    }

    protected override void Set(float input, bool sendCallback)
    {
        float previousValue = SliderValue;
        if (steps > 1 && maxValue > minValue)
        {
            float fraction = Mathf.InverseLerp(minValue, maxValue, input);
            float step = Mathf.Round(fraction * (steps - 1));
            input = Mathf.Lerp(minValue, maxValue, step / (steps - 1));
        }

        base.Set(input, sendCallback);
        Refresh();
        if (sendCallback && previousValue != SliderValue)
        {
            valueDidChangeEvent?.Invoke(this, SliderValue);
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
