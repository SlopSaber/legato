#if LEGATO_EARLY_VR_UI
using System;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Components;

public sealed class EarlyColorPanel : MonoBehaviour
{
    private RangeValuesTextSlider[] channels;
    private bool usesHsv;
    private Color currentColor = Color.white;

    public event Action<Color> colorDidChangeEvent;

    public Color color
    {
        get => currentColor;
        set
        {
            currentColor = value;
            if (channels == null)
            {
                return;
            }

            if (usesHsv)
            {
                Color.RGBToHSV(value, out float h, out float s, out float v);
                if (s > 0f)
                {
                    channels[0].SetWithoutNotification(h);
                }

                if (v > 0f)
                {
                    channels[1].SetWithoutNotification(s);
                }

                channels[2].SetWithoutNotification(v);
            }
            else
            {
                channels[0].SetWithoutNotification(value.r);
                channels[1].SetWithoutNotification(value.g);
                channels[2].SetWithoutNotification(value.b);
            }
        }
    }

    internal void Bind(RangeValuesTextSlider[] sliders, bool hsv)
    {
        channels = sliders;
        usesHsv = hsv;
        color = currentColor;
        foreach (RangeValuesTextSlider slider in channels)
        {
            slider.valueDidChangeEvent += OnChange;
        }
    }

    private void OnChange(TextSlider slider, float value)
    {
        float a = currentColor.a;
        float first = channels[0].value;
        float second = channels[1].value;
        float third = channels[2].value;
        currentColor = usesHsv ? Color.HSVToRGB(first, second, third) : new Color(first, second, third);
        currentColor.a = a;
        colorDidChangeEvent?.Invoke(currentColor);
    }
}

internal static class EarlyColorLayout
{
    internal static EarlyColorPanel CreatePanel(Transform parent, bool hsv)
    {
        string title = hsv ? "HSV" : "RGB";
        RectTransform panel = EarlyScrollLayout.CreateRect(parent, title);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(45f, 32f);
        panel.anchoredPosition = new Vector2(hsv ? 25f : -25f, 0f);

        TextMeshProUGUI heading = EarlySettingControlLayout.CreateLabel(panel, "Heading");
        heading.text = title;
        heading.alignment = TextAlignmentOptions.Center;
        heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        heading.rectTransform.sizeDelta = new Vector2(45f, 6f);
        heading.rectTransform.anchoredPosition = new Vector2(0f, -1f);

        RangeValuesTextSlider[] channels = new RangeValuesTextSlider[3];
        for (int i = 0; i < channels.Length; i++)
        {
            GameObject row = EarlySettingControlLayout.CreateSlider(panel, out channels[i], out TextMeshProUGUI label, 45f);
            label.text = title[i].ToString();
            RectTransform rect = (RectTransform)row.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 6f - (i * 8.5f));
            rect.sizeDelta = new Vector2(45f, 8f);

            int maximum = hsv ? (i == 0 ? 360 : 100) : 255;
            channels[i].numberOfSteps = maximum + 1;
            channels[i].SetFormatter(value => Mathf.RoundToInt(value * maximum).ToString());
            row.SetActive(true);
        }

        EarlyColorPanel controller = panel.gameObject.AddComponent<EarlyColorPanel>();
        controller.Bind(channels, hsv);
        return controller;
    }

    internal static Button CreateActionButton(Transform parent, string title, float offset)
    {
        Button button = EarlyButtonFactory.Create(parent, title);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(30f, 8f);
        rect.anchoredPosition = new Vector2(offset, -24f);
        EarlyButtonFactory.AddLabel(button).text = title;
        return button;
    }
}
#endif
