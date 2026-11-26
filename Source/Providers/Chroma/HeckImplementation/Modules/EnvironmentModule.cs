using System.Collections.Generic;
using System.Linq;
using Chroma.EnvironmentEnhancement.Saved;
using Chroma.Settings;
using Heck;
using Heck.Module;
#if !PRE_V1_37_1
using CustomJSONData;
#endif
using static Chroma.ChromaController;
#if !PRE_V1_37_1
using _EnvironmentType = EnvironmentType;
#else
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using _EnvironmentType = EnvironmentTypeSO;
#endif

namespace Chroma.Modules;

[Module("ChromaEnvironment", 2, LoadType.Active, ["ChromaColorizer"])]
[ModulePatcher(HARMONY_ID + "Environment", PatchType.Environment)]
internal class EnvironmentModule : IModule
{
    private readonly Config _config;
    private readonly SavedEnvironmentLoader _savedEnvironmentLoader;
#if BEAT_SABER_1453
    private readonly NativeLightshowQuality _nativeLightshowQuality;
#endif
#if !PRE_V1_37_1
    private readonly EnvironmentsListModel _environmentsListModel;
#else
    private readonly CustomLevelLoader _customLevelLoader;
#endif

    private EnvironmentModule(
        Config config,
        SavedEnvironmentLoader savedEnvironmentLoader,
#if BEAT_SABER_1453
        NativeLightshowQuality nativeLightshowQuality,
#endif
#if !PRE_V1_37_1
        EnvironmentsListModel environmentsListModel)
#else
        CustomLevelLoader customLevelLoader)
#endif
    {
        _config = config;
        _savedEnvironmentLoader = savedEnvironmentLoader;
#if BEAT_SABER_1453
        _nativeLightshowQuality = nativeLightshowQuality;
#endif
#if !PRE_V1_37_1
        _environmentsListModel = environmentsListModel;
#else
        _customLevelLoader = customLevelLoader;
#endif
    }

    internal enum EnvironmentOverrideType
    {
        None,
        MapOverride,
        SavedOverride
    }

    internal bool Active { get; private set; }

    internal EnvironmentOverrideType OverrideType { get; private set; }

    [ModuleCallback]
    private void Callback(bool value)
    {
        Active = value;
    }

    [ModuleCondition]
    private bool ConditionEnvironment(
#if !PRE_V1_37_1
        BeatmapKey beatmapKey,
        BeatmapLevel beatmapLevel,
#else
        IDifficultyBeatmap difficultyBeatmap,
#endif
        ModuleManager.ModuleArgs moduleArgs,
        bool dependency)
    {
#if !PRE_V1_37_1
#if BEAT_SABER_1453
        EnvironmentName environmentName = beatmapLevel.GetEnvironmentName(in beatmapKey);
#elif BEAT_SABER_1_45_1
        EnvironmentName environmentName = beatmapLevel.GetEnvironmentName(
            beatmapKey.characteristic,
            beatmapKey.difficulty);
#else
        EnvironmentName environmentName = beatmapLevel.GetEnvironmentName(
            beatmapKey.beatmapCharacteristic,
            beatmapKey.difficulty);
#endif
        EnvironmentInfoSO environmentInfo =
            _environmentsListModel.GetEnvironmentInfoBySerializedNameSafe(environmentName);
#else
        EnvironmentInfoSO environmentInfo = difficultyBeatmap.GetEnvironmentInfo();
#endif
        _EnvironmentType type = environmentInfo.environmentType;

#if !PRE_V1_37_1
        // Vivify maps replace the scene with their own content and rely on their environment edits.
        bool vivify = dependency &&
                      (beatmapLevel.GetBeatmapCustomData(beatmapKey).Get<List<object>>("_requirements")?
                          .Any(requirement => requirement as string == "Vivify") ?? false);
        bool settingForce = vivify;
        if (!settingForce && _config.ForceMapEnvironmentWhenQualityLights)
        {
            settingForce = LightshowQuality.IsQuality(beatmapLevel, in beatmapKey);
#if BEAT_SABER_1453
            settingForce = settingForce || _nativeLightshowQuality.IsQuality(beatmapLevel, in beatmapKey);
#endif
        }
#elif LEGATO_CHROMA_LEGACY_LIGHTS
        CustomBeatmapData? customBeatmapData = difficultyBeatmap.beatmapData as CustomBeatmapData;
        bool settingForce = _config.ForceMapEnvironmentWhenQualityLights &&
                            (customBeatmapData?.hasAuthoredEvents ??
                             (difficultyBeatmap.beatmapData?.beatmapEventData.Length > 0));
#else
        bool settingForce = _config.ForceMapEnvironmentWhenQualityLights && dependency;
#endif

#if PRE_V1_37_1 && !LEGATO_CHROMA_LEGACY_LIGHTS
        Version3CustomBeatmapSaveData? customBeatmapSaveData = difficultyBeatmap.GetBeatmapSaveData();
#endif
        if (settingForce ||
            (!_config.EnvironmentEnhancementsDisabled &&
#if !PRE_V1_37_1
             // cant conditionally enable environment module without reading customdata
             dependency))
#elif LEGATO_CHROMA_LEGACY_LIGHTS
             customBeatmapData != null &&
             ((customBeatmapData.beatmapCustomData.Get<List<object>>(V2_ENVIRONMENT_REMOVAL)?.Count ?? 0) > 0 ||
              (customBeatmapData.customData.Get<List<object>>(V2_ENVIRONMENT)?.Count ?? 0) > 0 ||
              (customBeatmapData.customData.Get<List<object>>(ENVIRONMENT)?.Count ?? 0) > 0)))
#else
             customBeatmapSaveData != null &&
             ((customBeatmapSaveData.beatmapCustomData.Get<List<object>>(V2_ENVIRONMENT_REMOVAL)?.Count ?? 0) > 0 ||
              (customBeatmapSaveData.customData.Get<List<object>>(V2_ENVIRONMENT)?.Count ?? 0) > 0 ||
              (customBeatmapSaveData.customData.Get<List<object>>(ENVIRONMENT)?.Count ?? 0) > 0)))
#endif
        {
#if !PRE_V1_37_1
            // Loading Chroma features does not itself force the map environment.
            if (settingForce)
#else
            if (settingForce || dependency)
#endif
            {
                moduleArgs.OverrideEnvironmentSettings = null;
                OverrideType = EnvironmentOverrideType.MapOverride;
#if BEAT_SABER_1453
                // Native shows retain the game's lighting pipeline unless a mod requires Chroma.
                if (beatmapLevel.previewMediaData is StaticPreviewMediaData)
                {
                    return dependency;
                }
#endif

                return true;
            }
        }
        else if (moduleArgs.OverrideEnvironmentSettings != null)
        {
            SavedEnvironment? savedEnvironment = _savedEnvironmentLoader.SavedEnvironment;
            if (_config.CustomEnvironmentEnabled && savedEnvironment != null)
            {
#if !PRE_V1_37_1
                EnvironmentInfoSO overrideEnv =
                    _environmentsListModel.GetEnvironmentInfoBySerializedNameSafe(savedEnvironment.EnvironmentName);
#else
                EnvironmentInfoSO overrideEnv =
                    _customLevelLoader.LoadEnvironmentInfo(savedEnvironment.EnvironmentName, type);
#endif
                OverrideEnvironmentSettings newSettings = new()
                {
                    overrideEnvironments = true
                };
                newSettings.SetEnvironmentInfoForType(type, overrideEnv);
                moduleArgs.OverrideEnvironmentSettings = newSettings;
                OverrideType = EnvironmentOverrideType.SavedOverride;
                return true;
            }
        }

#if !PRE_V1_37_1
        // The checker also verifies that the map environment actually loaded.
        OverrideType = dependency ? EnvironmentOverrideType.MapOverride : EnvironmentOverrideType.None;
#else
        OverrideType = EnvironmentOverrideType.None;
#endif
        return dependency;
    }
}
