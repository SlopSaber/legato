#nullable enable
using System;
using System.Reflection;
using static PlayerSaveData;

namespace Legato.Gameplay {
    internal static class EnvironmentEffectsFilterAccess {
#if !BEAT_SABER_1_29_0
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly FieldInfo? Default = typeof(PlayerSpecificSettings).GetField("_environmentEffectsFilterDefaultPreset", Fields);
        private static readonly FieldInfo? ExpertPlus = typeof(PlayerSpecificSettings).GetField("_environmentEffectsFilterExpertPlusPreset", Fields);
#endif
        internal static bool Apply(object[] arguments, bool forceStatic) {
#if BEAT_SABER_1_29_0
            for (int index = 0; index < arguments.Length; index++) {
                if (arguments[index] is not EnvironmentEffectsFilterPreset preset) continue;
                if (forceStatic) arguments[index] = preset = EnvironmentEffectsFilterPreset.NoEffects;
                return preset == EnvironmentEffectsFilterPreset.NoEffects;
            }
            return false;
#else
            BeatmapKey key = default;
            PlayerSpecificSettings? settings = null;
            foreach (object argument in arguments) {
                if (argument is BeatmapKey beatmapKey) key = beatmapKey;
                else if (argument is PlayerSpecificSettings playerSettings) settings = playerSettings;
            }
            if (settings == null) return false;
            FieldInfo? field = key.difficulty == BeatmapDifficulty.ExpertPlus ? ExpertPlus : Default;
            var preset = field?.GetValue(settings) is EnvironmentEffectsFilterPreset value ? value : EnvironmentEffectsFilterPreset.AllEffects;
            if (forceStatic) {
                Default?.SetValue(settings, EnvironmentEffectsFilterPreset.NoEffects);
                ExpertPlus?.SetValue(settings, EnvironmentEffectsFilterPreset.NoEffects);
                preset = EnvironmentEffectsFilterPreset.NoEffects;
            }
            return preset == EnvironmentEffectsFilterPreset.NoEffects;
#endif
        }
    }
}
