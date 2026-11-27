#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
#nullable enable
#if LEGATO_EARLY_STANDARD_MAPS
using System;
#endif
using HarmonyLib;
using VRUI;

namespace Legato.UI;

[HarmonyPatch(typeof(StandardLevelSelectionFlowCoordinator), nameof(StandardLevelSelectionFlowCoordinator.Present))]
internal static class EarlyPlaylistNavigation
{
#if LEGATO_EARLY_STANDARD_MAPS
    private static readonly Action<MainMenuViewController, MainMenuViewController.MenuButton> HandleMenuButton =
        (Action<MainMenuViewController, MainMenuViewController.MenuButton>)Delegate.CreateDelegate(
            typeof(Action<MainMenuViewController, MainMenuViewController.MenuButton>),
            AccessTools.Method(typeof(MainMenuViewController), "HandleMenuButton"));
#endif

    private static IStandardLevel[]? pendingLevels;
    private static IStandardLevel[]? activeLevels;
    private static StandardLevelSelectionFlowCoordinator? activeFlow;

    internal static bool TryPresent(MainMenuViewController menu, IStandardLevel[] levels)
    {
        if (!menu || !menu.isActiveAndEnabled || menu.childViewController != null || levels.Length == 0)
            return false;

        pendingLevels = levels;
        try
        {
            // The game records its Party route and restores it when returning from gameplay.
#if LEGATO_EARLY_STANDARD_MAPS
            HandleMenuButton(menu, MainMenuViewController.MenuButton.Party);
#else
            menu.PartyButtonPressed();
#endif
            return pendingLevels == null;
        }
        finally
        {
            pendingLevels = null;
        }
    }

    internal static void Clear()
    {
        if (activeFlow != null) activeFlow.didFinishEvent -= Clear;
        activeFlow = null;
        activeLevels = null;
    }

    private static void Prefix(StandardLevelSelectionFlowCoordinator __instance, VRUIViewController __0,
        ref IStandardLevel[] __1, GameplayMode __2)
    {
        if (__0 is not MainMenuViewController || __2 != GameplayMode.PartyStandard) return;

        IStandardLevel[]? playlist = __0.isRebuildingHierarchy ? activeLevels : pendingLevels;
        if (playlist == null)
        {
            if (!__0.isRebuildingHierarchy) Clear();
            return;
        }

        Clear();
        activeLevels = playlist;
        activeFlow = __instance;
        activeFlow.didFinishEvent += Clear;
        __1 = playlist;
        pendingLevels = null;
    }
}
#endif
