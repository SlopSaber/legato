#if LEGATO_SIRA_EARLY_CONTEXTS
using System.Collections.Generic;
using Legato.Sira;
using UnityEngine;

/// <summary>
/// Owns the visual model attached to a saber on the original scene layout.
/// </summary>
public class SaberModelController : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private readonly List<Glow> _glows = new();
    private readonly List<KeyValuePair<MeshRenderer, bool>> _stockRenderers = new();
    private readonly List<KeyValuePair<MeshRenderer, bool>> _hiddenRenderers = new();
    private readonly MaterialPropertyBlock _properties = new();
    private SaberWeaponTrail? _stockTrail;
    private Xft.XWeaponTrail? _ownedTrail;
    private Color _ownedTrailTint;
    private bool _stockTrailEnabled;
    private bool _stockHidden;

    internal Color PhysicalColor { get; private set; } = Color.white;

    /// <summary>
    /// Places a visual model at the origin of the supplied saber transform.
    /// </summary>
    public virtual void Init(Transform parent, Saber.SaberType saberType)
    {
        transform.SetParent(parent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    internal static SaberModelController ObserveStock(Saber saber, Color color)
    {
        GameObject root = new("SiraUtil Stock Saber Model");
        root.transform.SetParent(saber.transform, false);
        SaberModelController model = root.AddComponent<SaberModelController>();
        model.PhysicalColor = color;
        model._stockTrail = saber.GetComponent<SaberWeaponTrail>();
        foreach (MeshRenderer renderer in saber.GetComponentsInChildren<MeshRenderer>(true))
        {
            model._stockRenderers.Add(new KeyValuePair<MeshRenderer, bool>(renderer, renderer.enabled));
        }

        foreach (SetSaberGlowColor glow in saber.GetComponentsInChildren<SetSaberGlowColor>(true))
        {
            MeshRenderer renderer = EarlySaberNative.GetRenderer(glow);
            if (renderer != null)
            {
                model._glows.Add(new Glow(renderer, EarlySaberNative.GetTint(glow), glow));
            }
        }

        return model;
    }

    internal SaberModelController CopyStock(Saber source)
    {
        GameObject root = new("Stock Saber Model") { layer = source.gameObject.layer };
        root.SetActive(false);
        SaberModelController model = root.AddComponent<SaberModelController>();
        Dictionary<Transform, Transform> copies = new() { [source.transform] = root.transform };
        Dictionary<MeshRenderer, MeshRenderer> renderers = new();
        foreach (KeyValuePair<MeshRenderer, bool> state in _stockRenderers)
        {
            MeshRenderer renderer = state.Key;
            if (renderer == null)
            {
                continue;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            GameObject part = CopyTransform(renderer.transform, copies).gameObject;
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            MeshRenderer copy = part.AddComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            copy.shadowCastingMode = renderer.shadowCastingMode;
            copy.receiveShadows = renderer.receiveShadows;
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder;
            copy.enabled = state.Value;
            renderers.Add(renderer, copy);
        }

        foreach (Glow glow in _glows)
        {
            MeshRenderer sourceRenderer = glow.Renderer;
            if (sourceRenderer != null && renderers.TryGetValue(sourceRenderer, out MeshRenderer copy))
            {
                model._glows.Add(new Glow(copy, glow.Tint, null));
            }
        }

        return model;
    }

    internal void AttachNativeTrail(Saber saber, Saber template)
    {
        if (_ownedTrail == null && GetComponentInChildren<Xft.XWeaponTrail>(true) == null)
        {
            _ownedTrail = EarlySaberNative.CreateTrail(transform, template.GetComponent<SaberWeaponTrail>(), saber, out _ownedTrailTint);
            if (_ownedTrail != null)
            {
                EarlySaberNative.SetOwnedTrailColor(_ownedTrail, PhysicalColor * _ownedTrailTint);
                _ownedTrail.gameObject.SetActive(true);
            }
        }
    }

    internal void ApplyColor(Color color)
    {
        PhysicalColor = color;
        foreach (Glow glow in _glows)
        {
            glow.Apply(color, _properties);
        }

        if (_stockTrail != null)
        {
            EarlySaberNative.SetTrailColor(_stockTrail, color);
        }

        if (_ownedTrail != null)
        {
            EarlySaberNative.SetOwnedTrailColor(_ownedTrail, color * _ownedTrailTint);
        }
    }

    internal void HideStock()
    {
        if (_stockHidden)
        {
            return;
        }

        _stockHidden = true;
        foreach (KeyValuePair<MeshRenderer, bool> state in _stockRenderers)
        {
            MeshRenderer renderer = state.Key;
            if (renderer != null)
            {
                _hiddenRenderers.Add(new KeyValuePair<MeshRenderer, bool>(renderer, renderer.enabled));
                renderer.enabled = false;
            }
        }

        if (_stockTrail != null)
        {
            _stockTrailEnabled = _stockTrail.enabled;
            _stockTrail.enabled = false;
        }
    }

    internal void RestoreStock()
    {
        foreach (Glow glow in _glows)
        {
            glow.Restore();
        }

        if (_stockTrail != null)
        {
            EarlySaberNative.RestoreTrailColor(_stockTrail);
        }

        if (!_stockHidden)
        {
            return;
        }

        foreach (KeyValuePair<MeshRenderer, bool> state in _hiddenRenderers)
        {
            if (state.Key != null)
            {
                state.Key.enabled = state.Value;
            }
        }

        if (_stockTrail != null)
        {
            _stockTrail.enabled = _stockTrailEnabled;
        }

        _hiddenRenderers.Clear();
        _stockHidden = false;
    }

    private static Transform CopyTransform(Transform source, Dictionary<Transform, Transform> copies)
    {
        if (copies.TryGetValue(source, out Transform existing))
        {
            return existing;
        }

        Transform parent = CopyTransform(source.parent, copies);
        GameObject copy = new(source.name) { layer = source.gameObject.layer };
        copy.transform.SetParent(parent, false);
        copy.transform.localPosition = source.localPosition;
        copy.transform.localRotation = source.localRotation;
        copy.transform.localScale = source.localScale;
        copy.SetActive(source.gameObject.activeSelf);
        Rotate rotate = source.GetComponent<Rotate>();
        if (rotate != null)
        {
            EarlySaberNative.CopyRotation(rotate, copy);
        }

        copies.Add(source, copy.transform);
        return copy.transform;
    }

    private sealed class Glow
    {
        private readonly MeshRenderer _renderer;
        private readonly Color _tint;
        private readonly SetSaberGlowColor? _native;
        private MaterialPropertyBlock? _original;
        private bool _wasEnabled;

        internal Glow(MeshRenderer renderer, Color tint, SetSaberGlowColor? native)
        {
            _renderer = renderer;
            _tint = tint;
            _native = native;
        }

        internal MeshRenderer Renderer => _renderer;

        internal Color Tint => _tint;

        internal void Apply(Color color, MaterialPropertyBlock properties)
        {
            if (_renderer == null)
            {
                return;
            }

            if (_native != null && _original == null)
            {
                _original = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_original);
                _wasEnabled = _native.enabled;
                _native.enabled = false;
            }

            _renderer.GetPropertyBlock(properties);
            properties.SetColor(ColorProperty, color * _tint);
            _renderer.SetPropertyBlock(properties);
        }

        internal void Restore()
        {
            if (_original != null && _native != null && _renderer != null)
            {
                _renderer.SetPropertyBlock(_original);
                _native.enabled = _wasEnabled;
                _original = null;
            }
        }
    }
}
#endif
