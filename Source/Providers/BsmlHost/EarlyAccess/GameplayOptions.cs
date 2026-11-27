#if LEGATO_EARLY_VR_UI
using IPA.Utilities;
using UnityEngine;

namespace BeatSaberMarkupLanguage.GameplaySetup;

internal sealed class EarlyGameplayOptions : MonoBehaviour
{
    private MenuType menuType = MenuType.Solo;

    internal static MenuType GetMenuType(GameplayOptionsViewController view)
    {
        EarlyGameplayOptions context = view.GetComponent<EarlyGameplayOptions>();
        return context != null ? context.menuType : MenuType.Solo;
    }

    internal static void SetStandard(StandardLevelSelectionFlowCoordinator flow)
    {
        StandardLevelDetailViewController detail =
            flow.GetField<StandardLevelDetailViewController, StandardLevelSelectionFlowCoordinator>("_levelDetailViewController");
        SetMenuType(detail, MenuType.Solo);
    }

    internal static void SetCustom(CustomLevelSelectionFlowCoordinator flow)
    {
        StandardLevelDetailViewController detail =
            flow.GetField<StandardLevelDetailViewController, CustomLevelSelectionFlowCoordinator>("_songDetailViewController");
        SetMenuType(detail, MenuType.Custom);
    }

    private static void SetMenuType(StandardLevelDetailViewController detail, MenuType type)
    {
        if (detail == null)
        {
            return;
        }

        GameplayOptionsViewController view =
            detail.GetField<GameplayOptionsViewController, StandardLevelDetailViewController>("_gameplayOptionsViewController");
        if (view != null)
        {
            EarlyGameplayOptions context = view.GetComponent<EarlyGameplayOptions>();
            if (context == null)
            {
                context = view.gameObject.AddComponent<EarlyGameplayOptions>();
            }

            context.menuType = type;
        }
    }
}
#endif
