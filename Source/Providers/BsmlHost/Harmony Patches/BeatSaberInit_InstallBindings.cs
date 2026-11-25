#if !BEAT_SABER_1_29_0
using BeatSaberMarkupLanguage.Animations;
using HarmonyLib;
using Zenject;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    /// <summary>
    /// Registers the animation controller with the game's app container.
    /// </summary>
    [HarmonyPatch(typeof(BeatSaberInit), nameof(BeatSaberInit.InstallBindings))]
    internal static class BeatSaberInit_InstallBindings
    {
        private static void Prefix(BeatSaberInit __instance)
        {
            DiContainer container = __instance.Container;
            container.Bind(typeof(AnimationController), typeof(ITickable)).To<AnimationController>().AsSingle();
        }
    }
}
#endif
