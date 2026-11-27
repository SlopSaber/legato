#if LEGATO_EARLY_VR_UI
using System;

namespace UnityEngine;

internal static class EarlyComponentLookup
{
    internal static bool TryGetComponent<T>(this GameObject target, out T component)
        where T : Component
    {
        component = target.GetComponent<T>();
        return component != null;
    }

    internal static bool TryGetComponent<T>(this Component target, out T component)
        where T : Component
    {
        component = target.GetComponent<T>();
        return component != null;
    }

    internal static bool TryGetComponent(this GameObject target, Type type, out Component component)
    {
        component = target.GetComponent(type);
        return component != null;
    }
}
#endif
