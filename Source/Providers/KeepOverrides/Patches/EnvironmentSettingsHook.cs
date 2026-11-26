using KeepMyOverridesPls.Configuration;
using SiraUtil.Affinity;
#if LEGATO_BEFORE_MULTIPLAYER
using HMUI;
#endif

namespace KeepMyOverridesPls.Patches;

internal class EnvironmentSettingsHook : IAffinity
{
    private readonly PluginConfig config;

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
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

#if LEGATO_BEFORE_MULTIPLAYER
    [AffinityPostfix]
    [AffinityPatch(typeof(EnvironmentOverrideSettingsPanelController),
        nameof(EnvironmentOverrideSettingsPanelController.HandleDropDownDidSelectCellWithIdx))]
    private void SaveSelectedEnvironment(DropdownWithTableView dropDownWithTableView, int idx)
    {
        if (dropDownWithTableView.tableViewDataSource is not EnvironmentsTableViewDataSource source ||
            idx < 0 || idx >= source.environmentInfos.Count) return;
        var info = source.environmentInfos[idx];
        if (info == null) return;
        if (info.environmentType == files._normalEnvironmentType) config.NormalEnvironment = info.serializedName;
        else if (info.environmentType == files._a360DegreesEnvironmentType) config.CircleEnvironment = info.serializedName;
    }
#else
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
#endif
}
