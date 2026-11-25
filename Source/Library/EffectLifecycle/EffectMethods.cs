#nullable enable
namespace Legato.Rendering {
    internal static class EffectMethods {
        internal static string? EnvironmentName(GameplayCoreSceneSetupData? data) {
            if (data == null) return null;
#if LEGATO_SINGLE_BEATMAP_ENVIRONMENT
            return data.environmentInfo?.environmentName;
#else
            return data.targetEnvironmentInfo?.environmentName;
#endif
        }
        internal static bool IsPaused(PauseController controller) =>
#if BEAT_SABER_1_45_3
            IPA.Utilities.FieldAccessor<PauseController, PauseController.PauseState>.Get(controller, "_paused") == PauseController.PauseState.Paused;
#else
            IPA.Utilities.FieldAccessor<PauseController, bool>.Get(controller, "_paused");
#endif
        internal const string BurnMarksInitialize =
#if LEGATO_BURN_MARKS_START
            "Start";
#else
            "Initialize";
#endif
    }
}
