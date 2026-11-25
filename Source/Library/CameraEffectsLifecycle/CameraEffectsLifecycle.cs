using System.Collections.Generic;
using UnityEngine;

namespace Legato.Rendering
{
    internal static class CameraEffectsLifecycle
    {
        internal const string AttachMethod =
#if BEAT_SABER_1_42_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            "get_container";
#else
            "OnEnable";
#endif

        internal static IEnumerable<MainEffectController> ExistingControllers()
        {
#if BEAT_SABER_1_42_0 || BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            yield break;
#else
            // Scene cameras can be enabled before application affinity patches are installed.
            foreach (var controller in Resources.FindObjectsOfTypeAll<MainEffectController>())
            {
                if (controller.gameObject.scene.IsValid() && controller.isActiveAndEnabled &&
                    controller.gameObject.activeInHierarchy)
                {
                    yield return controller;
                }
            }
#endif
        }
    }
}
