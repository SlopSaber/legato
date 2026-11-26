#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage;
#else
using BeatSaberMarkupLanguage;
#endif
using UnityEngine;
#if LEGATO_HMUI_PLAIN_TMP
using NativeMenuText = TMPro.TextMeshProUGUI;
#else
using NativeMenuText = HMUI.CurvedTextMeshPro;
#endif

namespace Legato.Presentation {
    internal static class CurvedTextFactory {
        internal static NativeMenuText Create(RectTransform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta) {
#if BEAT_SABER_1_45_3
            return BeatSaberUI.CreateCurvedUIText(parent, text, anchorMin, anchorMax, anchoredPosition, sizeDelta);
#else
#pragma warning disable CS0618
            var label = (NativeMenuText)BeatSaberUI.CreateText(parent, text, anchoredPosition, sizeDelta);
#pragma warning restore CS0618
            label.rectTransform.anchorMin = anchorMin;
            label.rectTransform.anchorMax = anchorMax;
            label.rectTransform.anchoredPosition = anchoredPosition;
            label.rectTransform.sizeDelta = sizeDelta;
            return label;
#endif
        }

        internal static NativeMenuText Create(RectTransform parent, string text, Vector2 anchoredPosition) {
#pragma warning disable CS0618
            return (NativeMenuText)BeatSaberUI.CreateText(parent, text, anchoredPosition);
#pragma warning restore CS0618
        }
    }
}
