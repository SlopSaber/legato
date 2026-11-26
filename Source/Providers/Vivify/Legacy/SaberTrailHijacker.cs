#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System.Collections.Generic;
using UnityEngine;
using Xft;

namespace Vivify.ObjectPrefab.Hijackers;

internal sealed class SaberTrailHijacker : IHijacker<FollowedSaberTrail>
{
    private readonly XWeaponTrail _source;
    private Renderer? _hidden;
    private bool _previousForceOff;

    internal SaberTrailHijacker(XWeaponTrail source)
    {
        _source = source;
    }

    public void Activate(List<FollowedSaberTrail> trails, bool hideOriginal)
    {
        Transform parent = _source.transform.parent;
        foreach (FollowedSaberTrail trail in trails)
        {
            trail.transform.SetParent(parent, false);
            trail.Init(_source, parent);
        }

        if (hideOriginal && _source._trailRenderer != null)
        {
            _hidden = _source._trailRenderer._meshRenderer;
            _previousForceOff = _hidden.forceRenderingOff;
            _hidden.forceRenderingOff = true;
        }
    }

    public void Deactivate()
    {
        if (_hidden != null)
        {
            _hidden.forceRenderingOff = _previousForceOff;
        }

        _hidden = null;
    }
}
#endif
