#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
using HarmonyLib;
using Legato.XR;
using UnityEngine.XR.OpenXR;

namespace AssetBundleLoadingTools.Patches
{
    [HarmonyPatch(typeof(OpenXRSettings), nameof(OpenXRSettings.ApplyRenderSettings))]
    internal class OpenXRSettings_ApplyRenderSettings
    {
        public static void Prefix(OpenXRSettings __instance)
        {
            OpenXrRendering.SetRequestedMode(__instance, Plugin.Config.EnableMultiPassRendering);
        }
    }
}
#endif
