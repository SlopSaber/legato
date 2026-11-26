using KeepMyOverridesPls.Configuration;
#if LEGATO_BEFORE_MULTIPLAYER
using SiraUtil.Affinity;
#else
using SiraUtil.Logging;
#endif
using Zenject;
#if BEAT_SABER_1_29_0
using EnvironmentsListModel = global::EnvironmentsListSO;
#endif

namespace KeepMyOverridesPls;

#if LEGATO_BEFORE_MULTIPLAYER
internal class Overrider : IInitializable, IAffinity
{
    private readonly PluginConfig config;
    private readonly PlayerDataModel playerDataModel;

    public Overrider(PluginConfig config, PlayerDataModel playerDataModel)
    {
        this.config = config;
        this.playerDataModel = playerDataModel;
    }

    public void Initialize()
    {
        var settings = playerDataModel.playerData?.overrideEnvironmentSettings;
        if (settings != null) Apply(playerDataModel._playerDataFileManager, settings);
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(PlayerDataModel), nameof(PlayerDataModel.Load))]
    private void ApplyLoadedPreferences(PlayerDataModel __instance)
    {
        if (__instance == playerDataModel) Initialize();
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(PlayerDataFileManagerSO), nameof(PlayerDataFileManagerSO.CreateDefaultOverrideEnvironmentSettings))]
    private void ApplyDefaultPreferences(PlayerDataFileManagerSO __instance, OverrideEnvironmentSettings __result)
    {
        if (__instance == playerDataModel._playerDataFileManager) Apply(__instance, __result);
    }

    private void Apply(PlayerDataFileManagerSO files, OverrideEnvironmentSettings settings)
    {
        settings.overrideEnvironments = config.OverrideEnvironments;
        var catalog = files._allEnvironmentInfos;
        SetPreference(settings, catalog, files._normalEnvironmentType, config.NormalEnvironment);
        SetPreference(settings, catalog, files._a360DegreesEnvironmentType, config.CircleEnvironment);
    }

    private static void SetPreference(OverrideEnvironmentSettings settings, EnvironmentsListSO catalog,
        EnvironmentTypeSO type, string serializedName)
    {
        if (string.IsNullOrEmpty(serializedName)) return;
        var info = catalog.GetEnviromentInfoBySerializedName(serializedName);
        if (info != null && info.environmentType == type) settings.SetEnvironmentInfoForType(type, info);
    }
}
#else
internal class Overrider : IInitializable
{
    private readonly PluginConfig config;
    private readonly OverrideEnvironmentSettings overrideEnvironmentSettings;
    private readonly EnvironmentsListModel environmentsListModel;
    private readonly SiraLog logger;

    public Overrider(
        PluginConfig config,
        PlayerDataModel playerDataModel,
        EnvironmentsListModel environmentsListModel,
        SiraLog logger)
    {
        this.config = config;
        overrideEnvironmentSettings = playerDataModel.playerData.overrideEnvironmentSettings;
        this.environmentsListModel = environmentsListModel;
        this.logger = logger;
    }

    public void Initialize()
    {
        overrideEnvironmentSettings.overrideEnvironments = config.OverrideEnvironments;
#if BEAT_SABER_1_29_0
        SetEnvInfo(config.NormalEnvironment);
        SetEnvInfo(config.CircleEnvironment);
#else
        SetEnvInfoForType(EnvironmentType.Normal, config.NormalEnvironment);
        SetEnvInfoForType(EnvironmentType.Circle, config.CircleEnvironment);
#endif
    }

#if BEAT_SABER_1_29_0
    private void SetEnvInfo(string serializedName)
    {
        var info = environmentsListModel.GetEnvironmentInfoBySerializedName(serializedName);
        if (info == null)
        {
            logger.Warn($"Saved environment '{serializedName}' is unavailable; keeping the current override.");
            return;
        }
        overrideEnvironmentSettings.SetEnvironmentInfoForType(info.environmentType, info);
    }
#else
    private void SetEnvInfoForType(EnvironmentType type, string serializedName)
    {
        var envInfo = environmentsListModel.GetEnvironmentInfoBySerializedName(serializedName);
        if (envInfo == null)
        {
            logger.Warn($"Saved {type} environment '{serializedName}' is unavailable; keeping the current override.");
            return;
        }
        overrideEnvironmentSettings.SetEnvironmentInfoForType(type, envInfo);
    }
#endif
}
#endif
