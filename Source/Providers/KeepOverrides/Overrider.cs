using KeepMyOverridesPls.Configuration;
using SiraUtil.Logging;
using Zenject;
#if BEAT_SABER_1_29_0
using EnvironmentsListModel = global::EnvironmentsListSO;
#endif

namespace KeepMyOverridesPls;

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
