#nullable enable
using System.Linq;
using UnityEngine;
#if !BEAT_SABER_1_29_0
using BGLib.UnityExtension;
#endif

namespace Legato.Presentation {
    internal static class ScoreEffectResources {
        internal static FlyingTextEffect? LoadPreviewEffect() =>
#if BEAT_SABER_1_29_0
            Resources.FindObjectsOfTypeAll<FlyingTextEffect>()
                .Where(effect => effect.name == "FlyingTextEffect" || effect.name == "FlyingTextEffect(Clone)")
                .OrderBy(effect => effect.gameObject.scene.IsValid()).FirstOrDefault();
#else
            AddressablesExtensions.LoadContent<GameObject>("Assets/Prefabs/Effects/FlyingTextEffect.prefab")
                .FirstOrDefault()?.GetComponent<FlyingTextEffect>();
#endif
    }
}
