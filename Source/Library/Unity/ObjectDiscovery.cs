#nullable enable

using System;
using UnityEngine;

namespace Legato.Unity {
    internal static class ObjectDiscovery {
        internal static UnityEngine.Object FindFirstActive(Type type) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindFirstObjectByType(type);
#else
            return UnityEngine.Object.FindObjectOfType(type);
#endif
        }

        internal static T[] FindLoaded<T>() where T : UnityEngine.Object =>
            UnityEngine.Resources.FindObjectsOfTypeAll<T>();

        internal static T[] FindActiveUnsorted<T>() where T : UnityEngine.Object {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }

        internal static T[] FindActive<T>() where T : UnityEngine.Object {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }
    }
}
