#nullable enable

using BeatSaberMarkupLanguage;
using HMUI;
using UnityEngine;

namespace Legato.Presentation {
    internal static class CurvedTextFactory {
        internal static CurvedTextMeshPro Create(RectTransform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta) {
            return BeatSaberUI.CreateCurvedUIText(parent, text, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        }

        internal static CurvedTextMeshPro Create(RectTransform parent, string text, Vector2 anchoredPosition) {
#pragma warning disable CS0618
            return (CurvedTextMeshPro)BeatSaberUI.CreateText(parent, text, anchoredPosition);
#pragma warning restore CS0618
        }
    }
}
