using Zenject;
#if LEGATO_EARLY_STANDARD_MAPS
using UnityEngine.SceneManagement;
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections.Generic;
using UnityEngine;
#endif

namespace Legato.Models;

internal static class SaberGameplayBindings
{
#if LEGATO_BEFORE_MULTIPLAYER && !LEGATO_EARLY_STANDARD_MAPS
    internal static Saber LeftSaber(SaberManager manager) => manager._leftSaber;
    internal static Saber RightSaber(SaberManager manager) => manager._rightSaber;
#endif

    internal static void BindObstacleEvents(DiContainer container)
    {
#if LEGATO_EARLY_STANDARD_MAPS
        var scene = SceneManager.GetSceneByName("GameCore");
        if (!scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            var manager = root.GetComponentInChildren<ObstacleSaberSparkleEffectManager>(true);
            if (manager == null) continue;
            container.BindInstance(manager).IfNotBound();
            return;
        }
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        container.Bind<ObstacleSaberSparkleEffectManager>()
            .FromMethod(context => FindObstacleEvents(context.Container))
            .AsSingle()
            .IfNotBound();
#endif
    }

#if (BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER) && !LEGATO_EARLY_STANDARD_MAPS
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
