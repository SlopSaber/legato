using System.Collections.Generic;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Controllers;
using Vivify.PostProcessing;
using Zenject;

namespace Vivify.HarmonyPatches;

internal class AddComponentsToCamera : IAffinity, IInitializable
{
    private readonly SiraLog _log;
    private readonly DiContainer _container;

    private readonly List<Component> _injected = [];

    [UsedImplicitly]
    private AddComponentsToCamera(SiraLog log, DiContainer container)
    {
        _log = log;
        _container = container;
    }

    public void Initialize()
    {
        foreach (MainEffectController controller in Legato.Rendering.CameraEffectsLifecycle.ExistingControllers())
        {
            AddComponents(controller);
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(MainEffectController), Legato.Rendering.CameraEffectsLifecycle.AttachMethod)]
    private void AddComponents(MainEffectController __instance)
    {
        GameObject gameObject = __instance.gameObject;
#if !V1_29_1
        SafeAddComponent<MultipassKeywordController>(gameObject);
#endif
        SafeAddComponent<PostProcessingController>(gameObject);
        SafeAddComponent<CameraPropertyController>(gameObject);
    }

    private void SafeAddComponent<T>(GameObject gameObject)
        where T : Component
    {
        string name = typeof(T).Name;
        T found = gameObject.GetComponent<T>();
        if (found != null)
        {
            if (_injected.Contains(found))
            {
                return;
            }

            // likely the component was duplicated without being injected by zenject
            _log.Debug($"Injected [{name}] for [{gameObject.name}]");
            _container.Inject(found);
            _injected.Add(found);
            return;
        }

        _log.Debug($"Created [{name}] for [{gameObject.name}]");
        _injected.Add(_container.InstantiateComponent<T>(gameObject));
    }
}
