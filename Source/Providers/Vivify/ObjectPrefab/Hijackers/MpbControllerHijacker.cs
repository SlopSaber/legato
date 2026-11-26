using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Vivify.ObjectPrefab.Hijackers;

internal class MpbControllerHijacker : IHijacker<GameObject>
{
    private readonly Transform _child;
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
    private readonly MaterialPropertyBlockController? _materialPropertyBlockController;
    private readonly Dictionary<Renderer, bool> _hidden = new();
#else
    private readonly MaterialPropertyBlockController _materialPropertyBlockController;
#endif
    private readonly Renderer?[] _originalRenderers;
#if LEGATO_CACHED_RENDERER_MATERIAL_COUNTS && !LEGATO_VIVIFY_LEGACY_GAMEPLAY
    private List<int>? _cachedNumberOfMaterialsInRenderers;
#endif
    private Renderer[]? _cachedRenderers;

    [UsedImplicitly]
    internal MpbControllerHijacker(Component component)
    {
        _originalRenderers = component.GetComponentsInChildren<Renderer>();
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        _child = component switch
        {
            NoteController note => note.noteTransform,
            NoteDebris debris => debris._meshTransform,
            _ => component.transform
        };
        _materialPropertyBlockController = component is NoteDebris noteDebris
            ? noteDebris._materialPropertyBlockController
            : _child.GetComponentInChildren<MaterialPropertyBlockController>() ??
              component.GetComponentInChildren<MaterialPropertyBlockController>();
#else
        _child = component.transform.GetChild(0);

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (component is GameNoteController or BurstSliderGameNoteController)
        {
            _materialPropertyBlockController =
                component.transform.GetChild(0).GetComponent<MaterialPropertyBlockController>();
        }
        else
        {
            _materialPropertyBlockController = component.GetComponent<MaterialPropertyBlockController>();
        }
#endif
    }

    public void Activate(List<GameObject> gameObjects, bool hideOriginal)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            gameObject.transform.SetParent(_child, false);
        }

#if LEGATO_CACHED_RENDERER_MATERIAL_COUNTS && !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        if (_materialPropertyBlockController._isInitialized)
        {
            _cachedNumberOfMaterialsInRenderers =
                _materialPropertyBlockController._numberOfMaterialsInRenderers;
            _materialPropertyBlockController._isInitialized = false;
        }
#endif

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        if (hideOriginal)
        {
            foreach (Renderer? renderer in _originalRenderers)
            {
                if (renderer != null)
                {
                    _hidden[renderer] = renderer.forceRenderingOff;
                    renderer.forceRenderingOff = true;
                }
            }
        }

        if (_materialPropertyBlockController == null)
        {
            return;
        }
#endif

        _cachedRenderers = _materialPropertyBlockController._renderers;
        IEnumerable<Renderer> newRenderers =
            gameObjects.SelectMany(n => n.GetComponentsInChildren<Renderer>(true));

        if (hideOriginal)
        {
#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
            foreach (Renderer? renderer in _originalRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

#endif
            _materialPropertyBlockController._renderers = newRenderers.ToArray();
        }
        else
        {
            _materialPropertyBlockController._renderers = _cachedRenderers.Concat(newRenderers).ToArray();
        }

        _materialPropertyBlockController.ApplyChanges();
    }

    public void Deactivate()
    {
#if LEGATO_CACHED_RENDERER_MATERIAL_COUNTS && !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        if (_cachedNumberOfMaterialsInRenderers != null)
        {
            _materialPropertyBlockController._numberOfMaterialsInRenderers =
                _cachedNumberOfMaterialsInRenderers;
            _cachedNumberOfMaterialsInRenderers = null;
        }
#endif

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        if (_cachedRenderers != null && _materialPropertyBlockController != null)
#else
        if (_cachedRenderers != null)
#endif
        {
            _materialPropertyBlockController._renderers = _cachedRenderers;
            _cachedRenderers = null;
        }

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        foreach (KeyValuePair<Renderer, bool> hidden in _hidden)
        {
            if (hidden.Key != null)
            {
                hidden.Key.forceRenderingOff = hidden.Value;
            }
        }

        _hidden.Clear();
#else
        // other mods (looking at you, NoteCutGuide) can cause some of the original renderers to be destroyed
        foreach (Renderer? renderer in _originalRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
#endif
    }
}
