#nullable enable
namespace Legato.Rendering {
    internal static class EffectMethods {
        internal static string? EnvironmentName(GameplayCoreSceneSetupData? data) {
            if (data == null) return null;
#if BEAT_SABER_1_29_0
            return data.environmentInfo?.environmentName;
#else
            return data.targetEnvironmentInfo?.environmentName;
#endif
        }
        internal const string BurnMarksInitialize =
#if BEAT_SABER_1_29_0
            "Start";
#else
            "Initialize";
#endif
    }
}
