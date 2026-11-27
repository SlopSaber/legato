using System;
using System.Reflection;
#if !LEGATO_SIRA_EARLY_CONTEXTS
using HMUI;
#endif
using UnityEngine.Networking;

namespace Legato.Sira
{
    internal static class NativeAccess
    {
        internal static void BindNativeServices(Zenject.DiContainer container)
        {
#if LEGATO_SIRA_EARLY_CONTEXTS
            if (!container.HasBinding<MainSettingsModel>())
            {
                foreach (var initializer in UnityEngine.Resources.FindObjectsOfTypeAll<MainSystemInit>())
                {
                    if (initializer.gameObject.scene.IsValid() && MainSettings.GetValue(initializer) is MainSettingsModel settings)
                    {
                        container.BindInstance(settings);
                        break;
                    }
                }
            }
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            if (!container.HasBinding<MainSettingsModelSO>())
                container.Bind<MainSettingsModelSO>().FromInstance(FindMainSettings()).AsSingle();
            if (!container.HasBinding<ColorSchemesSettings>())
                container.Bind<ColorSchemesSettings>().FromMethod(context =>
                    context.Container.Resolve<PlayerDataModel>().playerData.colorSchemesSettings).AsSingle();
            if (!container.HasBinding<EnvironmentsListSO>())
                container.Bind<EnvironmentsListSO>().FromMethod(context =>
                    (EnvironmentsListSO)EnvironmentCatalog.GetValue(context.Container.Resolve<CustomLevelLoader>())!)
                    .AsSingle();
#endif
        }

        internal static bool MatchesInstaller(Type expected, Type actual)
        {
#if !LEGATO_SIRA_EARLY_CONTEXTS && (BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER)
            if (expected == typeof(AppInit)) return expected.IsAssignableFrom(actual);
#endif
            return expected == actual;
        }

#if LEGATO_SIRA_EARLY_CONTEXTS || BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private static readonly FieldInfo MainSettings = typeof(MainSystemInit).GetField(
            "_mainSettingsModel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(MainSystemInit).FullName, "_mainSettingsModel");

#if !LEGATO_SIRA_EARLY_CONTEXTS
        private static MainSettingsModelSO FindMainSettings()
        {
            foreach (var initializer in UnityEngine.Resources.FindObjectsOfTypeAll<MainSystemInit>())
                if (initializer.gameObject.scene.IsValid()
                    && MainSettings.GetValue(initializer) is MainSettingsModelSO settings) return settings;
            throw new InvalidOperationException("The native application settings are not loaded.");
        }

        private static readonly FieldInfo EnvironmentCatalog = typeof(CustomLevelLoader).GetField(
#if LEGATO_BEFORE_MULTIPLAYER
            "_enviromentSceneInfoColection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(CustomLevelLoader).FullName, "_enviromentSceneInfoColection");
#else
            "_environmentSceneInfoCollection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(CustomLevelLoader).FullName, "_environmentSceneInfoCollection");
#endif
#endif
#endif

#if !LEGATO_SIRA_EARLY_CONTEXTS
#if LEGATO_HMUI_ACTIVATION_ENUMS
        private static readonly Action<FlowCoordinator, ViewController?, bool> SetBottomScreen =
            (Action<FlowCoordinator, ViewController?, bool>)Delegate.CreateDelegate(
                typeof(Action<FlowCoordinator, ViewController?, bool>),
                typeof(FlowCoordinator).GetMethod("SetBottomScreenViewController", BindingFlags.Instance | BindingFlags.NonPublic)!);

        internal static void SetBottomScreenViewController(FlowCoordinator flow, ViewController? view) =>
            SetBottomScreen(flow, view, true);
#else
#if LEGATO_SIRA_LEGACY_VIEW_SYSTEM
        private static readonly Action<FlowCoordinator, ViewController?, ViewController.AnimationType> SetBottomScreen =
            (Action<FlowCoordinator, ViewController?, ViewController.AnimationType>)Delegate.CreateDelegate(
                typeof(Action<FlowCoordinator, ViewController?, ViewController.AnimationType>),
                typeof(FlowCoordinator).GetMethod("SetBottomScreenViewController", BindingFlags.Instance | BindingFlags.NonPublic)!);
#endif

        internal static void SetBottomScreenViewController(FlowCoordinator flow, ViewController? view, ViewController.AnimationType animation)
        {
#if LEGATO_SIRA_LEGACY_VIEW_SYSTEM
            SetBottomScreen(flow, view, animation);
#else
            flow.SetBottomScreenViewController(view, animation);
#endif
        }
#endif

        internal static bool IsPaused(PauseController controller)
        {
#if LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
            return controller._paused;
#else
            return controller._paused == PauseController.PauseState.Paused;
#endif
        }

#else
        private static readonly FieldInfo SaberHistory = typeof(Saber).GetField(
            "_historyData", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(Saber).FullName, "_historyData");
#endif

        internal static float LastMovementTime(Saber saber)
        {
#if LEGATO_SIRA_EARLY_CONTEXTS
            return SaberHistory.GetValue(saber) is SaberHistoryData history ? history.lastAddedTimeAndPos.time : 0f;
#elif LEGATO_BEFORE_MULTIPLAYER
            return saber._movementData.lastAddedData.time;
#elif BEAT_SABER_1_29_0 || (BEAT_SABER_1_37_1 && !LEGATO_SEPARATE_SABER_LOGIC)
            return saber.movementData.lastAddedData.time;
#else
            return saber.movementDataForLogic.lastAddedData.time;
#endif
        }

        internal static bool RequestSucceeded(UnityWebRequest request)
        {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            return request.isDone && !request.isNetworkError && !request.isHttpError;
#else
            return request.isDone && request.result == UnityWebRequest.Result.Success;
#endif
        }
    }
}
