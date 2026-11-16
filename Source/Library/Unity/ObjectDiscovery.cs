#nullable enable

using System;
#if BEAT_SABER_1_29_0
using System.Linq;
#endif
using UnityEngine;

namespace Legato.Unity {
    internal static class ObjectDiscovery {
        internal static T? FindInParentsIncludingInactive<T>(Component component) where T : Component {
            for (Transform? current = component != null ? component.transform : null; current != null; current = current.parent) {
                T candidate = current.GetComponent<T>();
                if (candidate != null) return candidate;
            }
            return null;
        }

        internal static T FindFirstActive<T>() where T : UnityEngine.Object {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        internal static UnityEngine.Object FindFirstActive(Type type) {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindFirstObjectByType(type);
#else
            return UnityEngine.Object.FindObjectOfType(type);
#endif
        }

        internal static T FindAnyActive<T>() where T : UnityEngine.Object {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindAnyObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        internal static T[] FindIncludingInactiveUnsorted<T>() where T : UnityEngine.Object {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#elif BEAT_SABER_1_29_0
            if (!typeof(Component).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(GameObject))
                return UnityEngine.Object.FindObjectsOfType<T>();
            return UnityEngine.Resources.FindObjectsOfTypeAll<T>()
                .Where(value => (value.hideFlags & HideFlags.DontSave) == 0 &&
                    (value is Component component ? component.gameObject.scene.IsValid() :
                     value is GameObject gameObject && gameObject.scene.IsValid()))
                .OrderBy(value => value.GetInstanceID()).ToArray();
#else
            return UnityEngine.Object.FindObjectsOfType<T>(true);
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
