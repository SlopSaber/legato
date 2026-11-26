#if LEGATO_BEFORE_MULTIPLAYER
using System;
using IPA.Utilities;

namespace Legato.Gameplay {
    internal static class LegacyReplayLifecycle {
        internal static void InitializeTransition(StandardLevelScenesTransitionSetupDataSO transition,
            IDifficultyBeatmap difficulty, PlayerData player, EnvironmentInfoSO? environment,
            GameplayModifiers modifiers, PlayerSpecificSettings settings, PracticeSettings? practice) {
            var environmentSettings = player.overrideEnvironmentSettings;
            var environmentType = difficulty.GetEnvironmentInfo().environmentType;
            if (environment != null) {
                environmentSettings = new OverrideEnvironmentSettings { overrideEnvironments = true };
                environmentSettings.SetEnvironmentInfoForType(environmentType, environment);
            } else if (environmentSettings?.GetOverrideEnvironmentInfoForType(environmentType) == null) {
                environmentSettings = null;
            }

            var schemes = player.colorSchemesSettings;
            var colorScheme = schemes.overrideDefaultColors ? schemes.GetSelectedColorScheme() : null;
            transition.Init(difficulty, environmentSettings, colorScheme, modifiers, settings, practice, "Menu");
        }

        internal static PlayerSpecificSettings PlayerSettings(PlayerSpecificSettings settings, bool leftHanded,
            float? height) => new() {
            staticLights = settings.staticLights,
            leftHanded = leftHanded,
            playerHeight = height ?? settings.playerHeight,
            automaticPlayerHeight = !height.HasValue,
            sfxVolume = settings.sfxVolume,
            reduceDebris = settings.reduceDebris,
            noTextsAndHuds = settings.noTextsAndHuds,
            noFailEffects = settings.noFailEffects,
            advancedHud = settings.advancedHud,
            autoRestart = false,
            saberTrailIntensity = settings.saberTrailIntensity
        };

        internal static bool IsPaused(PauseController controller) => controller._paused;
        internal static bool CanPause(PauseController controller) => controller.canPause;

        internal static void SetPaused(PauseController controller, PlayerController player, bool paused,
            bool notifyListeners) {
            NativeGameState.SetPaused(controller, paused);
            if (paused) {
                controller._gamePause.Pause();
                player.disableSabers = false;
            } else {
                controller._gamePause.Resume();
            }

            if (notifyListeners) {
                controller.GetField<Action, PauseController>(paused ? "didPauseEvent" : "didResumeEvent")?.Invoke();
            }
        }
    }
}
#endif
