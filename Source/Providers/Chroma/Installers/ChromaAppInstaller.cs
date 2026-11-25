using Chroma.EnvironmentEnhancement;
using Chroma.EnvironmentEnhancement.Saved;
using Chroma.HarmonyPatches;
using Chroma.HarmonyPatches.ZenModeWalls;
using Chroma.Modules;
using Chroma.Settings;
using JetBrains.Annotations;
using Legato.Injection;
using Zenject;

namespace Chroma.Installers;

[UsedImplicitly]
internal class ChromaAppInstaller : Installer
{
    private readonly Config _config;

    private ChromaAppInstaller(Config config)
    {
        _config = config;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(_config);

        Container.Bind<SavedEnvironmentLoader>().AsSingle();
        ComponentBinding.OnNewGameObject(Container.Bind<EnvironmentMaterialsManager>()).AsSingle();
        Container.BindInterfacesTo<SavedEnvironmentLoading>().AsSingle();

        Container.BindInterfacesTo<ForceZenModeObstacleBeatmapData>().AsSingle();

#if BEAT_SABER_1453
        Container.BindInterfacesAndSelfTo<NativeLightshowQuality>().AsSingle();
#endif

        Container.BindInterfacesAndSelfTo<ColorizerModule>().AsSingle();
        Container.BindInterfacesAndSelfTo<FeaturesModule>().AsSingle();
        Container.BindInterfacesAndSelfTo<EnvironmentModule>().AsSingle();
    }
}
