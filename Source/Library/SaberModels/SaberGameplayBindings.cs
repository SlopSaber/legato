using Zenject;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections.Generic;
using UnityEngine;
#endif

namespace Legato.Models;

internal static class SaberGameplayBindings
{
#if LEGATO_BEFORE_MULTIPLAYER
    internal static Saber LeftSaber(SaberManager manager) => manager._leftSaber;
    internal static Saber RightSaber(SaberManager manager) => manager._rightSaber;
#endif

    internal static void BindObstacleEvents(DiContainer container)
    {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        container.Bind<ObstacleSaberSparkleEffectManager>()
            .FromMethod(context => FindObstacleEvents(context.Container))
            .AsSingle()
            .IfNotBound();
#endif
    }

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
    private static ObstacleSaberSparkleEffectManager FindObstacleEvents(DiContainer container)
    {
        var roots = new HashSet<GameObject>(container.Resolve<Context>().GetRootGameObjects());
        // Decorator components share the container but can belong to separate scenes.
        foreach (var decorator in container.ResolveAll<SceneDecoratorContext>())
        {
            if (decorator == null) continue;
            foreach (var root in decorator.gameObject.scene.GetRootGameObjects()) roots.Add(root);
        }
        foreach (var root in roots)
        {
            var manager = root.GetComponentInChildren<ObstacleSaberSparkleEffectManager>(true);
            if (manager != null) return manager;
        }
        throw new InvalidOperationException("The gameplay context has no obstacle saber effect manager.");
    }
#endif
}
