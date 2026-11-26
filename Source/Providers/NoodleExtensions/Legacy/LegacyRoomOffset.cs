#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using UnityEngine;

namespace NoodleExtensions.Managers;

internal sealed class LegacyRoomOffset : MonoBehaviour
{
    private Vector3SO _center = null!;
    private FloatSO _rotation = null!;

    internal void Initialize(VRCenterAdjust adjust)
    {
        _center = adjust._roomCenter;
        _rotation = adjust._roomRotation;
        _center.didChangeEvent += Refresh;
        _rotation.didChangeEvent += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        transform.localPosition = _center.value;
        transform.localRotation = Quaternion.Euler(0f, _rotation.value, 0f);
    }

    private void OnDestroy()
    {
        _center.didChangeEvent -= Refresh;
        _rotation.didChangeEvent -= Refresh;
    }
}
#endif
