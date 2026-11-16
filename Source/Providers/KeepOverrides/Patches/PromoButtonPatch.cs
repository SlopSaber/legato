using SiraUtil.Affinity;

namespace KeepMyOverridesPls.Patches;

internal class PromoButtonPatch : IAffinity
{
    // normally, the game will try to reset your override settings when pressing the promo button in the main menu.
    // this is done in order to ensure you use any potential new environments and colors for new content.
    // this patch makes sure the environments stay as they are.
        
#if BEAT_SABER_1_29_0
    private const string PromoMethod = nameof(MainFlowCoordinator.HandleMainMenuViewControllerMusicPackPromoButtonWasPressed);
#else
    private const string PromoMethod = nameof(MainFlowCoordinator.HandleMainMenuViewControllerPromoButtonWasPressed);
#endif

    private bool overrideEnvironments;
    private bool overrideDefaultColors;

    [AffinityPrefix]
    [AffinityPatch(typeof(MainFlowCoordinator), PromoMethod)]
    private void HandleMainMenuViewControllerPromoButtonWasPressedPrefix(PlayerDataModel ____playerDataModel)
    {
        overrideEnvironments = ____playerDataModel.playerData.overrideEnvironmentSettings.overrideEnvironments;
        overrideDefaultColors = ____playerDataModel.playerData.colorSchemesSettings.overrideDefaultColors;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(MainFlowCoordinator), PromoMethod)]
    private void HandleMainMenuViewControllerPromoButtonWasPressedPostfix(ref PlayerDataModel ____playerDataModel)
    {
        ____playerDataModel.playerData.overrideEnvironmentSettings.overrideEnvironments = overrideEnvironments;
        ____playerDataModel.playerData.colorSchemesSettings.overrideDefaultColors = overrideDefaultColors;
    }
}