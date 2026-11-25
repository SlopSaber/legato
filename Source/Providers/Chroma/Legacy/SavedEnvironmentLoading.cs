#if LEGATO_CHROMA_LEGACY_LIGHTS
using System.Collections.Generic;
using System.Reflection;
using Chroma.EnvironmentEnhancement.Saved;
using Chroma.Settings;
using CustomJSONData.CustomBeatmap;
using SiraUtil.Affinity;
using static Chroma.ChromaController;

namespace Chroma.HarmonyPatches;

internal sealed class SavedEnvironmentLoading : IAffinity
{
    private static readonly MethodInfo _copySettings = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly Config _config;
    private readonly SavedEnvironmentLoader _loader;

    private SavedEnvironmentLoading(Config config, SavedEnvironmentLoader loader)
    {
        _config = config;
        _loader = loader;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(BeatDataTransformHelper), nameof(BeatDataTransformHelper.CreateTransformedBeatmapData))]
    private void ApplyEffectsFilter(BeatmapData beatmapData, ref PlayerSpecificSettings playerSpecificSettings)
    {
        if (!playerSpecificSettings.staticLights && GetFeatures(beatmapData) is { ForceStaticLights: true })
        {
            playerSpecificSettings = (PlayerSpecificSettings)_copySettings.Invoke(playerSpecificSettings, null);
            playerSpecificSettings.staticLights = true;
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(BeatDataTransformHelper), nameof(BeatDataTransformHelper.CreateTransformedBeatmapData))]
    private void ApplyDefaultEvents(BeatmapData beatmapData, PlayerSpecificSettings playerSpecificSettings, ref BeatmapData __result)
    {
        if (beatmapData is not CustomBeatmapData { hasAuthoredEvents: false } || playerSpecificSettings.staticLights ||
            GetFeatures(beatmapData)?.BasicEvents is not { Length: > 0 } events || __result is not CustomBeatmapData transformed)
        {
            return;
        }

        __result = transformed.CopyWithEvents((BeatmapEventData[])events.Clone());
    }

    private Features? GetFeatures(BeatmapData beatmapData)
    {
        if (!_config.CustomEnvironmentEnabled)
        {
            return null;
        }

        if (!_config.EnvironmentEnhancementsDisabled && beatmapData is CustomBeatmapData custom &&
            (HasEntries(custom.beatmapCustomData, V2_ENVIRONMENT_REMOVAL) ||
             HasEntries(custom.customData, V2_ENVIRONMENT) || HasEntries(custom.customData, ENVIRONMENT)))
        {
            return null;
        }

        return _loader.SavedEnvironment?.Features;
    }

    private static bool HasEntries(CustomData data, string key) => data.Get<List<object>>(key) is { Count: > 0 };
}
#endif
