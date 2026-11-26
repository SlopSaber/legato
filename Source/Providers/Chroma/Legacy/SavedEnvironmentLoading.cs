#if LEGATO_CHROMA_LEGACY_LIGHTS
using System.Reflection;
using Chroma.EnvironmentEnhancement.Saved;
using Chroma.Modules;
using CustomJSONData.CustomBeatmap;
using SiraUtil.Affinity;

namespace Chroma.HarmonyPatches;

internal sealed class SavedEnvironmentLoading : IAffinity
{
    private static readonly MethodInfo _copySettings = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly EnvironmentModule _environmentModule;
    private readonly SavedEnvironmentLoader _loader;

    private SavedEnvironmentLoading(EnvironmentModule environmentModule, SavedEnvironmentLoader loader)
    {
        _environmentModule = environmentModule;
        _loader = loader;
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(BeatDataTransformHelper), nameof(BeatDataTransformHelper.CreateTransformedBeatmapData))]
    private void ApplyEffectsFilter(ref PlayerSpecificSettings playerSpecificSettings)
    {
        if (!playerSpecificSettings.staticLights && GetFeatures() is { ForceStaticLights: true })
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
            GetFeatures()?.BasicEvents is not { Length: > 0 } events || __result is not CustomBeatmapData transformed)
        {
            return;
        }

        __result = transformed.CopyWithEvents((BeatmapEventData[])events.Clone());
    }

    private Features? GetFeatures()
    {
        return _environmentModule.OverrideType == EnvironmentModule.EnvironmentOverrideType.SavedOverride
            ? _loader.SavedEnvironment?.Features
            : null;
    }
}
#endif
