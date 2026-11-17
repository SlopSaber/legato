using System.Collections.Generic;
using UnityEngine;

namespace Legato.Rendering
{
    internal static class CameraEffectsLifecycle
    {
        internal const string AttachMethod =
#if BEAT_SABER_1_29_0
            "OnEnable";
#else
            "get_container";
#endif

        internal static IEnumerable<MainEffectController> ExistingControllers()
        {
#if BEAT_SABER_1_29_0
            // Scene cameras can be enabled before application affinity patches are installed.
            foreach (var controller in Resources.FindObjectsOfTypeAll<MainEffectController>())
            {
                if (controller.gameObject.scene.IsValid() && controller.isActiveAndEnabled &&
                    controller.gameObject.activeInHierarchy)
                {
                    yield return controller;
                }
            }
#else
            yield break;
#endif
        }
    }
}
