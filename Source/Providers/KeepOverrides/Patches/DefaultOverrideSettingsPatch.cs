#if !LEGATO_BEFORE_MULTIPLAYER
using KeepMyOverridesPls.Configuration;
using SiraUtil.Affinity;
#if BEAT_SABER_1_29_0
using EnvironmentsListModel = global::EnvironmentsListSO;
#endif

namespace KeepMyOverridesPls.Patches;

internal class DefaultOverrideSettingsPatch : IAffinity
{
    private readonly PluginConfig config;
    private readonly EnvironmentsListModel environmentsListModel;

    public DefaultOverrideSettingsPatch(PluginConfig config, EnvironmentsListModel environmentsListModel)
    {
        this.config = config;
        this.environmentsListModel = environmentsListModel;
    }

#if BEAT_SABER_1_29_0
    [AffinityPostfix]
    [AffinityPatch(typeof(PlayerDataFileManagerSO), nameof(PlayerDataFileManagerSO.CreateDefaultOverrideEnvironmentSettings))]
    private void ApplyDefaults(PlayerDataFileManagerSO __instance, OverrideEnvironmentSettings __result)
    {
        __result.overrideEnvironments = config.OverrideEnvironments;
        var normal = environmentsListModel.GetEnvironmentInfoBySerializedName(config.NormalEnvironment);
        var circle = environmentsListModel.GetEnvironmentInfoBySerializedName(config.CircleEnvironment);
        if (normal != null) __result.SetEnvironmentInfoForType(__instance._normalEnvironmentType, normal);
        if (circle != null) __result.SetEnvironmentInfoForType(__instance._a360DegreesEnvironmentType, circle);
    }
#else
    [AffinityPrefix]
    [AffinityPatch(typeof(PlayerDataFileModel), nameof(PlayerDataFileModel.CreateDefaultOverrideEnvironmentSettings))]
    private bool CreateDefaultOverrideEnvironmentSettingsPrefix(ref OverrideEnvironmentSettings __result)
    {
        var overrideEnvironmentSettings = Legato.Gameplay.OverrideEnvironmentSettingsFactory.CreateEmpty(config.OverrideEnvironments);
        overrideEnvironmentSettings.SetEnvironmentInfoForType(EnvironmentType.Normal, environmentsListModel.GetEnvironmentInfoBySerializedName(config.NormalEnvironment));
        overrideEnvironmentSettings.SetEnvironmentInfoForType(EnvironmentType.Circle, environmentsListModel.GetEnvironmentInfoBySerializedName(config.CircleEnvironment));
        __result = overrideEnvironmentSettings;
        return false;
    }
#endif
}
#endif
