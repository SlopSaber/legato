using Zenject;

namespace Legato.Models;

internal static class SaberGameplayBindings
{
    internal static void BindObstacleEvents(DiContainer container)
    {
#if BEAT_SABER_1_29_0
        container.Bind<ObstacleSaberSparkleEffectManager>()
            .FromComponentInHierarchy()
            .AsSingle()
            .IfNotBound();
#endif
    }
}
