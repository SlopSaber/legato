#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Vivify.ObjectPrefab.Hijackers;

internal sealed class SaberModelControllerHijacker : IHijacker<GameObject>
{
    private readonly SaberModelContainer _container;
    private readonly IInstantiator _instantiator;
    private readonly Dictionary<Renderer, bool> _hidden = new();
    private BasicSaberModelController? _model;
    private SetSaberGlowColor[]? _originalColors;

    internal SaberModelControllerHijacker(SaberModelContainer container, IInstantiator instantiator)
    {
        _container = container;
        _instantiator = instantiator;
    }

    public void Activate(List<GameObject> gameObjects, bool hideOriginal)
    {
        Transform parent = _container.transform;
        SaberType type = _container._saberTypeObject.saberType;
        _model = _container._saberModelController as BasicSaberModelController;
        if (hideOriginal && _container._saberModelController is Component original)
        {
            foreach (Renderer renderer in original.GetComponentsInChildren<Renderer>())
            {
                _hidden[renderer] = renderer.forceRenderingOff;
                renderer.forceRenderingOff = true;
            }
        }

        var colors = new SetSaberGlowColor[gameObjects.Count];
        for (int index = 0; index < colors.Length; index++)
        {
            GameObject prefab = gameObjects[index];
            prefab.transform.SetParent(parent, false);
            SetSaberManyColor color = prefab.GetComponent<SetSaberManyColor>() ??
                                      _instantiator.InstantiateComponent<SetSaberManyColor>(prefab);
            color.saberType = type;
            colors[index] = color;
        }

        if (_model != null)
        {
            _originalColors = _model._setSaberGlowColors;
            _model._setSaberGlowColors = hideOriginal ? colors : _originalColors.Concat(colors).ToArray();
        }
    }

    public void Deactivate()
    {
        if (_model != null && _originalColors != null)
        {
            _model._setSaberGlowColors = _originalColors;
        }

        _model = null;
        _originalColors = null;
        foreach (KeyValuePair<Renderer, bool> hidden in _hidden)
        {
            if (hidden.Key != null)
            {
                hidden.Key.forceRenderingOff = hidden.Value;
            }
        }

        _hidden.Clear();
    }
}
#endif
