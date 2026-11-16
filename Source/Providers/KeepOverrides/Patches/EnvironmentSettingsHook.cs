using KeepMyOverridesPls.Configuration;
using SiraUtil.Affinity;

namespace KeepMyOverridesPls.Patches;

internal class EnvironmentSettingsHook : IAffinity
{
    private readonly PluginConfig config;

#if BEAT_SABER_1_29_0
    private readonly PlayerDataFileManagerSO files;

    public EnvironmentSettingsHook(PluginConfig config, PlayerDataModel playerDataModel)
    {
        this.config = config;
        files = playerDataModel._playerDataFileManager;
    }
#else
    public EnvironmentSettingsHook(PluginConfig config)
    {
        this.config = config;
    }

    #endif

    [AffinityPostfix]
    [AffinityPatch(typeof(OverrideEnvironmentSettings), nameof(OverrideEnvironmentSettings.SetEnvironmentInfoForType))]
#if BEAT_SABER_1_29_0
    private void SetEnvironmentInfoForTypePostFix(EnvironmentTypeSO environmentType, EnvironmentInfoSO environmentInfo)
    {
        if (environmentInfo == null) return;
        if (environmentType == files._normalEnvironmentType) config.NormalEnvironment = environmentInfo.serializedName;
        else if (environmentType == files._a360DegreesEnvironmentType) config.CircleEnvironment = environmentInfo.serializedName;
    }
#else
    private void SetEnvironmentInfoForTypePostFix(EnvironmentType environmentType, EnvironmentInfoSO environmentInfo)
    {
        // this gets called when the player selects an environment from the dropdown
        switch (environmentType)
        {
            case EnvironmentType.Normal:
                config.NormalEnvironment = environmentInfo.serializedName; break;

            case EnvironmentType.Circle:
                config.CircleEnvironment = environmentInfo.serializedName; break;
        }
    }
#endif
}
