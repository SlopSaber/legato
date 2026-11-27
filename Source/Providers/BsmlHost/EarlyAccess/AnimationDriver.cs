#if LEGATO_EARLY_VR_UI
using UnityEngine;

namespace BeatSaberMarkupLanguage.Animations;

internal sealed class EarlyAnimationDriver : MonoBehaviour
{
    private AnimationController controller;

    internal static void StartDriver()
    {
        GameObject owner = new("BSML Animation Driver");
        DontDestroyOnLoad(owner);
        EarlyAnimationDriver driver = owner.AddComponent<EarlyAnimationDriver>();
        driver.controller = AnimationController.CreateEarly();
        driver.controller.Initialize();
    }

    private void Update() => controller.Tick();
}
#endif
