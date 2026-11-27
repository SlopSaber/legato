#nullable enable

using System;
using System.Reflection;
using UnityEngine;

namespace Legato.Unity {
    internal static class GameplayCompatibility {
#if LEGATO_EARLY_STANDARD_MAPS
        internal static void Resume(GamePauseManager controller) => controller.ResumeGame();

        internal static MenuSceneSetupData? FindActiveMenuTransitions() =>
            UnityEngine.Resources.Load<MenuSceneSetupData>("scenesetup/MenuSceneSetupData");
#else
        private const BindingFlags InstanceMethods = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private const string ResumeMethod = "Resume";
#else
        private const string ResumeMethod = "HandlePauseMenuManagerDidPressContinueButton";
#endif
        private static readonly Action<PauseController> ResumeAction = (Action<PauseController>)
            typeof(PauseController).GetMethod(ResumeMethod, InstanceMethods)!.CreateDelegate(typeof(Action<PauseController>));

        internal static void Resume(PauseController controller) => ResumeAction(controller);

        internal static MenuTransitionsHelper? FindActiveMenuTransitions() {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            foreach (MainFlowCoordinator coordinator in UnityEngine.Resources.FindObjectsOfTypeAll<MainFlowCoordinator>()) {
                if (coordinator == null || !coordinator.gameObject.activeInHierarchy) continue;
                var helper = (MenuTransitionsHelper?)MenuTransitionsField.GetValue(coordinator);
                if (helper != null) return helper;
            }
#endif
            return null;
        }

#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
        private static readonly FieldInfo MenuTransitionsField = typeof(MainFlowCoordinator).GetField("_menuTransitionsHelper", BindingFlags.Instance | BindingFlags.NonPublic)!;
#endif
#endif
    }
}
