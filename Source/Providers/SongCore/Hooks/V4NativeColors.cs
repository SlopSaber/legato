#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Linq;
using IPA.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SongCore.Hooks
{
    internal static class V4NativeColors
    {
        internal static ColorScheme? Apply(ColorScheme current, IDifficultyBeatmap key)
        {
            if (key?.level == null || !Loader.NativeSaveStore.TryGetValue(key.level.levelID, out var data) || data.beatmapLevelSaveData == null) return null;
            var descriptor = data.beatmapLevelSaveData.Difficulties.FirstOrDefault(value => value.Characteristic == key.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName && value.Name == key.difficulty.ToString());
            if (descriptor == null || descriptor.ColorSchemeIndex < 0 || data.beatmapLevelSaveData.Document["colorSchemes"] is not JArray schemes
                || descriptor.ColorSchemeIndex >= schemes.Count || schemes[descriptor.ColorSchemeIndex] is not JObject source) return null;
            bool notes = source.Value<bool?>("overrideNotes") ?? false;
            bool lights = source.Value<bool?>("overrideLights") ?? false;
            if (!notes && !lights) return null;
            Color Read(string name, Color fallback, bool enabled)
            {
                if (!enabled || source[name] == null) return fallback;
                string value = source.Value<string>(name) ?? string.Empty;
                if (!value.StartsWith("#", StringComparison.Ordinal)) value = "#" + value;
                return ColorUtility.TryParseHtmlString(value, out var color) ? color : fallback;
            }
            var result = new ColorScheme("SongCoreV4ColorScheme", "SongCore v4 Color Scheme", true, "SongCore v4 Color Scheme", false,
                Read("saberAColor", current.saberAColor, notes), Read("saberBColor", current.saberBColor, notes),
                Read("environmentColor0", current.environmentColor0, lights), Read("environmentColor1", current.environmentColor1, lights),
                lights || current.supportsEnvironmentColorBoost,
                Read("environmentColor0Boost", current.environmentColor0Boost, lights), Read("environmentColor1Boost", current.environmentColor1Boost, lights),
                Read("obstaclesColor", current.obstaclesColor, notes));
            result.SetField("_environmentColorW", Read("environmentColorW", current.environmentColorW, lights));
            result.SetField("_environmentColorWBoost", Read("environmentColorWBoost", current.environmentColorWBoost, lights));
            return result;
        }
    }
}
#endif
