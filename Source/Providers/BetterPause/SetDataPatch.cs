using System;
using SiraUtil.Affinity;
#if BEAT_SABER_1_29_0
using Zenject;
#else
using System.Threading.Tasks;
#endif

namespace BetterPause.Patches
{
    internal partial class SetDataPatch
#if BEAT_SABER_1_29_0
        : IInitializable, IDisposable
#endif
    {
#if BEAT_SABER_1_29_0
        private IDisposable _nativeSetupSubscription;

        public void Initialize() => _nativeSetupSubscription = Legato.Presentation.LegacyPauseBarHooks.Subscribe(Postfix);
        public void Dispose() => _nativeSetupSubscription?.Dispose();
#else
        [AffinityPostfix]
        [AffinityPatch(typeof(LevelBar), "Setup", AffinityMethodType.Normal, null, new[] { typeof(NativePauseLevel), typeof(BeatmapDifficulty), typeof(NativePauseCharacteristic) })]
        private void NativeSetupPostfix(LevelBar __instance, NativePauseLevel beatmapLevel) => Postfix(__instance, beatmapLevel);

        [AffinityPostfix]
        [AffinityPatch(typeof(LevelBar), "SetupData", AffinityMethodType.Normal, null, new[] { typeof(NativePauseLevel), typeof(BeatmapDifficulty), typeof(NativePauseCharacteristic) })]
        private void NativeSetupDataPostfix(LevelBar __instance, NativePauseLevel beatmapLevel, ref Task __result) => SetupDataPostfix(__instance, beatmapLevel, ref __result);
#endif
    }
}
