using HMUI;
using TMPro;
using UnityEngine;
#if LEGATO_HMUI_ACTIVATION_ENUMS
using CurvedTextMeshPro = TMPro.TextMeshProUGUI;
#endif

namespace SiraUtil.Submissions
{
    internal class SiraSubmissionViewController : ViewController
    {
        private CurvedTextMeshPro? _curvedText;

#if LEGATO_HMUI_ACTIVATION_ENUMS
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            base.DidActivate(firstActivation, activationType);
            if (firstActivation)
            {
                GameObject textGameObject = new("SiraSubmissionText", typeof(RectTransform));
                textGameObject.layer = gameObject.layer;
                textGameObject.SetActive(false);
                textGameObject.transform.SetParent(transform, false);
                _curvedText = textGameObject.AddComponent<CurvedTextMeshPro>();
                RectTransform rect = _curvedText.rectTransform;
                rect.sizeDelta = new Vector2(40f, 100f);
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.anchoredPosition = new Vector2(0f, -51f);
                _curvedText.alignment = TextAlignmentOptions.Center;
                _curvedText.lineSpacing = -45f;
                _curvedText.fontSize = 3.5f;
            }
        }
#else
#if LEGATO_SIRA_LEGACY_VIEW_SYSTEM
        protected override void DidActivate
#else
        public override void DidActivate
#endif
            (bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (firstActivation)
            {
                GameObject textGameObject = gameObject;
                _curvedText = textGameObject.AddComponent<CurvedTextMeshPro>();
                textGameObject.transform.SetParent(transform);
                (textGameObject.transform as RectTransform)!.sizeDelta = new Vector2(40f, 100f);
                (textGameObject.transform as RectTransform)!.anchorMin = new Vector2(0f, 0f);
                (textGameObject.transform as RectTransform)!.anchorMax = new Vector2(0f, 0f);
                textGameObject.transform.localPosition = new Vector2(0f, -51f);
                textGameObject.transform.localScale = Vector3.one;
                _curvedText.alignment = TextAlignmentOptions.Center;
                _curvedText.lineSpacing = -45f;
                _curvedText.fontSize = 3.5f;
                _curvedText.gameObject.SetActive(false);
            }
        }
#endif

        public void Enabled(bool value)
        {
            if (_curvedText != null)
            {
                _curvedText.gameObject.SetActive(value);
            }
        }

        public void SetText(string text)
        {
            if (_curvedText != null)
            {
                _curvedText.text = text;
            }
        }
    }
}
