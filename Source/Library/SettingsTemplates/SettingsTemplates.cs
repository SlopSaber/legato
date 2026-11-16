using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
#if !BEAT_SABER_1_29_0
using BeatSaber.GameSettings;
#endif

namespace Legato.UI {
    internal static class SettingsTemplates {
        internal static GameObject PositionSlider(DiContainer container) {
#if BEAT_SABER_1_29_0
            return Resources.FindObjectsOfTypeAll<LayoutElement>()
                .First(x => x.name == "PositionX").gameObject;
#else
            return container.Resolve<MainSettingsMenuViewController>()._settingsSubMenuInfos
                .First(x => x.viewController is ControllerProfilesSettingsViewController)
                .viewController.transform.Find("Content/MainContent/Sliders/PositionX").gameObject;
#endif
        }

        internal static GameObject AudioLatencyToggle(DiContainer container) =>
            ((AudioLatencyViewController)container.Resolve<MainSettingsMenuViewController>()
                ._settingsSubMenuInfos.First(x => x.viewController is AudioLatencyViewController)
                .viewController)._overrideAudioLatencyToggle.gameObject;
    }
}
