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
            Set(this.value, false);
        }
    }

    internal TextMeshProUGUI ValueLabel { get; private set; }

    public void Refresh()
    {
        if (ValueLabel != null)
        {
            ValueLabel.text = formatter != null ? formatter(this.value) : this.value.ToString("N2");
            decrement.interactable = interactable && this.value > minValue;
            increment.interactable = interactable && this.value < maxValue;
        }
    }

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
        RectTransform rect = (RectTransform)transform;
        rect.offsetMin = new Vector2(spacing, 0f);
        rect.offsetMax = new Vector2(-spacing, 0f);
        Refresh();
    }

    protected override void Set(float input, bool sendCallback)
    {
        float previousValue = this.value;
        if (steps > 1 && maxValue > minValue)
        {
            float fraction = Mathf.InverseLerp(minValue, maxValue, input);
            float step = Mathf.Round(fraction * (steps - 1));
            input = Mathf.Lerp(minValue, maxValue, step / (steps - 1));
        }

        base.Set(input, sendCallback);
        Refresh();
        if (sendCallback && previousValue != this.value)
        {
            valueDidChangeEvent?.Invoke(this, this.value);
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
