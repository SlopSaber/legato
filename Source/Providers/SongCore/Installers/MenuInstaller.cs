using Legato.Injection;
using SongCore.Hooks;
using SongCore.Hooks.BeatmapLevelCache;
using SongCore.UI;
using Zenject;

namespace SongCore.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<SettingsController>().AsSingle();
            Container.BindInterfacesAndSelfTo<Loader>().AsSingle();
            Container.BindInterfacesAndSelfTo<ColorsUI>().AsSingle();
            ComponentBinding.OnNewGameObject(Container.Bind<ProgressBar>()).AsSingle();
            Container.BindInterfacesAndSelfTo<RequirementsUI>().AsSingle();
            Container.Bind<BeatmapLevelCache>().AsSingle();
            Container.BindInterfacesTo<BeatmapVersionDetectionHook>().AsSingle();
#if BEAT_SABER_1_29_0
            Container.BindInterfacesTo<V4LoadingHooks>().AsSingle();
            Container.BindInterfacesTo<V4LightingData>().AsSingle();
#else
            Container.BindInterfacesTo<BeatmapJsonCacheHooks>().AsSingle();
#endif
            Container.BindInterfacesTo<BeatmapDataCacheHooks>().AsSingle();
            Container.BindInterfacesTo<SongDataMenuHooks>().AsSingle();
            Container.BindInterfacesTo<InternalRestartHook>().AsSingle();
            Container.BindInterfacesTo<BindBeatmapLevelHook>().AsSingle();
            Container.BindInterfacesTo<YeetLegacyBpmEventsHook>().AsSingle();
            Container.BindInterfacesTo<StandardLevelDetailViewControllerHook>().AsSingle();
            Container.BindInterfacesTo<RemoveDownloadIconHook>().AsSingle();
            Container.BindInterfacesTo<RefreshAfterEditorHook>().AsSingle();
            Container.BindInterfacesTo<LoadingHooks>().AsSingle();
            Container.BindInterfacesTo<LevelListTableCellDataHook>().AsSingle();
            Container.BindInterfacesTo<EscapeURLHook>().AsSingle();
            Container.BindInterfacesTo<ComputeMaxMultipliedScoreSafelyHook>().AsSingle();
            Container.BindInterfacesTo<IOBlacklistHook>().AsSingle();
        }
    }
}
