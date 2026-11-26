using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
#if !BEAT_SABER_1_29_0 && !BEAT_SABER_1_37_1 && !BEAT_SABER_1_38_0 && !LEGATO_BEFORE_MULTIPLAYER
using BeatSaber.GameSettings;
#endif

namespace Legato.UI {
    internal static class SettingsTemplates {
        internal static GameObject PositionSlider(DiContainer container) {
#if LEGATO_BEFORE_MULTIPLAYER
            return Resources.FindObjectsOfTypeAll<ControllersTransformSettingsViewController>()
                .First()._posXSlider.GetComponentInParent<LayoutElement>().gameObject;
#elif BEAT_SABER_1_29_0
            return Resources.FindObjectsOfTypeAll<LayoutElement>()
                .First(x => x.name == "PositionX").gameObject;
#elif BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
            return container.Resolve<MainSettingsMenuViewController>()._settingsSubMenuInfos
                .First(x => x.viewController is ControllersTransformSettingsViewController)
                .viewController.transform.Find("Content/PositionX").gameObject;
#else
            return container.Resolve<MainSettingsMenuViewController>()._settingsSubMenuInfos
                .First(x => x.viewController is ControllerProfilesSettingsViewController)
                .viewController.transform.Find("Content/MainContent/Sliders/PositionX").gameObject;
#endif
        }

        internal static GameObject AudioLatencyToggle(DiContainer container) =>
#if LEGATO_BEFORE_MULTIPLAYER
            Resources.FindObjectsOfTypeAll<AudioLatencyViewController>().First()._overrideAudioLatencyToggle.gameObject;
#else
            ((AudioLatencyViewController)container.Resolve<MainSettingsMenuViewController>()
                ._settingsSubMenuInfos.First(x => x.viewController is AudioLatencyViewController)
                .viewController)._overrideAudioLatencyToggle.gameObject;
#endif
    }
}
