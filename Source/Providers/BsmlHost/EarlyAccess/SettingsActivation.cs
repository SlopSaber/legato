#if LEGATO_EARLY_VR_UI
using BeatSaberMarkupLanguage.Settings;
using HarmonyLib;
using VRUI;

namespace BeatSaberMarkupLanguage.Harmony_Patches;

[HarmonyPatch(typeof(SettingsViewController), "DidActivate")]
internal static class EarlySettingsActivation
{
    private static void Prefix(SettingsViewController __instance, VRUIViewController.ActivationType activationType)
    {
        if (activationType == VRUIViewController.ActivationType.AddedToHierarchy)
        {
            BSMLSettings.Instance.AttachToSettings(__instance);
        }
    }
}

[HarmonyPatch(typeof(SettingsViewController), "ApplySettings")]
internal static class EarlySettingsApply
{
    private static void Postfix(SettingsViewController __instance) => BSMLSettings.Instance.FinishSettings(__instance, "apply");
}

[HarmonyPatch(typeof(SettingsViewController), "CancelSettings")]
internal static class EarlySettingsCancel
{
    private static void Postfix(SettingsViewController __instance) => BSMLSettings.Instance.FinishSettings(__instance, "cancel");
}
#endif
