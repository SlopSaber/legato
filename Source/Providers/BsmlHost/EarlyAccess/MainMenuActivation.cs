#if LEGATO_EARLY_VR_UI
using BeatSaberMarkupLanguage.Util;
using HarmonyLib;
using UnityEngine;

namespace BeatSaberMarkupLanguage.Harmony_Patches;

[HarmonyPatch(typeof(MainMenuViewController), "DidActivate")]
internal static class MainFlowCoordinator_DidActivate
{
    private static void Postfix(MainMenuViewController __instance, bool firstActivation)
    {
        if (firstActivation)
        {
            BSMLParser.InitializeEarly();
        }

        MenuButtons.MenuButtons.Instance.AttachToMenu(__instance);

        if (firstActivation)
        {
            __instance.gameObject.AddComponent<EarlyMenuLifetime>().Initialize();
        }
    }
}

internal sealed class EarlyMenuLifetime : MonoBehaviour
{
    private readonly MainMenuAwaiter awaiter = new();

    internal void Initialize() => awaiter.Initialize();

    private void OnDestroy() => awaiter.Dispose();
}
#endif
