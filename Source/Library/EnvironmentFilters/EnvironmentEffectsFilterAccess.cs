#nullable enable
using System;
using System.Reflection;
using static PlayerSaveData;

namespace Legato.Gameplay {
    internal static class EnvironmentEffectsFilterAccess {
#if LEGATO_BEFORE_MULTIPLAYER
        private static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo BeatmapEvents = typeof(BeatmapData).GetField("<beatmapEventData>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;

        internal static BeatmapData WithLightEvents(BeatmapData beatmap, BeatmapEventData[] events) {
            // Lighting changes leave the note lines, custom map data, and spawn rotation count intact.
            var copy = (BeatmapData)Clone.Invoke(beatmap, null)!;
            BeatmapEvents.SetValue(copy, events);
            return copy;
        }
#elif !BEAT_SABER_1_29_0
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly FieldInfo? Default = typeof(PlayerSpecificSettings).GetField("_environmentEffectsFilterDefaultPreset", Fields);
        private static readonly FieldInfo? ExpertPlus = typeof(PlayerSpecificSettings).GetField("_environmentEffectsFilterExpertPlusPreset", Fields);
#endif
        internal static bool Apply(object[] arguments, bool forceStatic) {
#if LEGATO_BEFORE_MULTIPLAYER
            for (int index = 0; index < arguments.Length; index++) {
                if (arguments[index] is not PlayerSpecificSettings settings) continue;
                if (!settings.staticLights && forceStatic) {
                    var copy = (PlayerSpecificSettings)Clone.Invoke(settings, null)!;
                    copy.staticLights = true;
                    arguments[index] = copy;
                }
                return forceStatic || settings.staticLights;
            }
            return false;
#elif BEAT_SABER_1_29_0
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
