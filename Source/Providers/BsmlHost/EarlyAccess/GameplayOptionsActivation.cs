#if LEGATO_EARLY_VR_UI
using BeatSaberMarkupLanguage.GameplaySetup;
using HarmonyLib;
using GameplaySetupHost = BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup;

namespace BeatSaberMarkupLanguage.Harmony_Patches;

[HarmonyPatch(typeof(StandardLevelSelectionFlowCoordinator), nameof(StandardLevelSelectionFlowCoordinator.Present))]
internal static class EarlyStandardGameplayOptions
{
    private static void Prefix(StandardLevelSelectionFlowCoordinator __instance) => EarlyGameplayOptions.SetStandard(__instance);
}

[HarmonyPatch(typeof(CustomLevelSelectionFlowCoordinator), nameof(CustomLevelSelectionFlowCoordinator.Present))]
internal static class EarlyCustomGameplayOptions
{
    private static void Prefix(CustomLevelSelectionFlowCoordinator __instance) => EarlyGameplayOptions.SetCustom(__instance);
}

[HarmonyPatch(typeof(GameplayOptionsViewController), "DidActivate")]
internal static class EarlyGameplayOptionsActivation
{
    private static void Postfix(GameplayOptionsViewController __instance) => GameplaySetupHost.Instance.AttachToOptions(__instance);
}
#endif
