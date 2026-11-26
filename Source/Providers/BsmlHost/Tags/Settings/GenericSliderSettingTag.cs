using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
#if !BEAT_SABER_1_29_0 && !BEAT_SABER_1_37_1 && !BEAT_SABER_1_38_0 && !LEGATO_HMUI_TABLE_SCROLLER
using BeatSaber.GameSettings;
#endif
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMarkupLanguage.Tags.Settings
{
    /// <summary>
    /// Builds slider settings from the game's controller settings template.
    /// </summary>
    /// <typeparam name="T">The component that supplies the slider behavior.</typeparam>
    public abstract class GenericSliderSettingTag<T> : BSMLTag
        where T : GenericSliderSetting
    {
        private LayoutElement controllersTransformTemplate;

        /// <inheritdoc />
        public override GameObject CreateObject(Transform parent)
        {
            if (this.controllersTransformTemplate == null)
            {
#if LEGATO_HMUI_TABLE_SCROLLER
                ControllersTransformSettingsViewController controller = Resources.FindObjectsOfTypeAll<ControllersTransformSettingsViewController>().First();
                this.controllersTransformTemplate = controller._posXSlider.transform.parent.GetComponent<LayoutElement>();
#elif BEAT_SABER_1_29_0
                this.controllersTransformTemplate = Resources.FindObjectsOfTypeAll<LayoutElement>().First(x => x.name == "PositionX");
#elif BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
                this.controllersTransformTemplate = this.DiContainer.Resolve<MainSettingsMenuViewController>()._settingsSubMenuInfos.First(m => m.viewController is ControllersTransformSettingsViewController).viewController.transform.Find("Content/PositionX").GetComponent<LayoutElement>();
#else
                this.controllersTransformTemplate = this.DiContainer.Resolve<MainSettingsMenuViewController>()._settingsSubMenuInfos.First(m => m.viewController is ControllerProfilesSettingsViewController).viewController.transform.Find("Content/MainContent/Sliders/PositionX").GetComponent<LayoutElement>();
#endif
            }

            LayoutElement baseSetting = Object.Instantiate(this.controllersTransformTemplate, parent, false);
            baseSetting.name = "BSMLSliderSetting";

            RectTransform rectTransform = (RectTransform)baseSetting.transform;
            rectTransform.anchoredPosition = Vector3.zero;

            Transform sliderLeft = rectTransform.Find("SliderLeft");
            if (sliderLeft != null)
            {
                Object.Destroy(sliderLeft.gameObject);
            }

            Object.Destroy(baseSetting.GetComponent<CanvasGroup>());

            GameObject gameObject = baseSetting.gameObject;
#if LEGATO_HMUI_LEGACY_GRAPHICS
            gameObject.SetActive(false);
#endif

            T sliderSetting = gameObject.AddComponent<T>();

#if LEGATO_HMUI_TABLE_SCROLLER
            sliderSetting.Slider = baseSetting.GetComponentInChildren<RangeValuesTextSlider>(true);
#else
            sliderSetting.Slider = baseSetting.GetComponentInChildren<CustomFormatRangeValuesSlider>();
#endif
            sliderSetting.Slider.name = "BSMLSlider";
            TextMeshProUGUI valueText = sliderSetting.Slider.GetComponentInChildren<TextMeshProUGUI>(true);
            if (valueText != null)
            {
                Legato.Unity.TextCompatibility.SetWrapping(valueText, false);
            }

            sliderSetting.Slider._enableDragging = true;

            RectTransform sliderTransform = (RectTransform)sliderSetting.Slider.transform;
            sliderTransform.anchorMin = new Vector2(1, 0);
            sliderTransform.anchorMax = new Vector2(1, 1);
            sliderTransform.sizeDelta = new Vector2(52, 0);
            sliderTransform.pivot = new Vector2(1, 0.5f);
            sliderTransform.anchoredPosition = new Vector2(0, 0);

            GameObject titleObject = gameObject.transform.Find("Title").gameObject;
            LocalizedTextMeshProUGUI localizedText = this.ConfigureLocalizedText(titleObject);

            RectTransform titleTransform = (RectTransform)titleObject.transform;
            titleTransform.anchorMin = Vector3.zero;
            titleTransform.anchorMax = Vector3.zero;
            titleTransform.offsetMin = Vector3.zero;
            titleTransform.offsetMax = new Vector2(-52, 0);

            TextMeshProUGUI titleTextMesh = titleObject.GetComponent<TextMeshProUGUI>();
            titleTextMesh.text = "Default Text";
            titleTextMesh.rectTransform.anchorMax = Vector2.one;
            titleTextMesh.alignment = TextAlignmentOptions.CaplineLeft;

            List<Component> externalComponents = gameObject.AddComponent<ExternalComponents>().Components;
            externalComponents.Add(titleTextMesh);
            externalComponents.Add(localizedText);

            baseSetting.preferredWidth = 90;

            gameObject.SetActive(true);
            return gameObject;
        }
    }
}
