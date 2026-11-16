#nullable enable

using System.Reflection;
using UnityEngine;

namespace Legato.Unity {
    internal static class GameplayCompatibility {
        internal static void Resume(PauseController controller) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            controller.Resume();
#else
            controller.HandlePauseMenuManagerDidPressContinueButton();
#endif
        }

        internal static MenuTransitionsHelper? FindActiveMenuTransitions() {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            foreach (MainFlowCoordinator coordinator in Resources.FindObjectsOfTypeAll<MainFlowCoordinator>()) {
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
    }
}
