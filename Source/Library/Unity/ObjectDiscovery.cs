#nullable enable

using System;
#if BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
using System.Linq;
#endif
using UnityEngine;

namespace Legato.Unity
{
    /// <summary>
    /// Finds Unity objects across the supported engine versions.
    /// </summary>
    internal static class ObjectDiscovery
    {
        /// <summary>
        /// Searches the component's own object and its parents, including inactive ones.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="component">The component whose hierarchy to search.</param>
        /// <returns>The nearest matching component, or null.</returns>
        internal static T? FindInParentsIncludingInactive<T>(Component component)
            where T : Component
        {
            for (Transform? current = component != null ? component.transform : null; current != null; current = current.parent)
            {
                T candidate = current.GetComponent<T>();
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the first active object in instance ID order.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The first match, or null.</returns>
        internal static T FindFirstActive<T>()
            where T : UnityEngine.Object
        {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3 || LEGATO_UNITY_6_OBJECT_QUERIES
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        /// <summary>
        /// Finds the first active object of a type selected at runtime.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <returns>The first match, or null.</returns>
        internal static UnityEngine.Object FindFirstActive(Type type)
        {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3 || LEGATO_UNITY_6_OBJECT_QUERIES
            return UnityEngine.Object.FindFirstObjectByType(type);
#else
            return UnityEngine.Object.FindObjectOfType(type);
#endif
        }

        /// <summary>
        /// Finds an active object without requiring a specific match order.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>A matching object, or null.</returns>
        internal static T FindAnyActive<T>()
            where T : UnityEngine.Object
        {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindAnyObjectByType<T>();
#elif LEGATO_UNITY_6_OBJECT_QUERIES
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        /// <summary>
        /// Finds scene objects, including inactive scene components and game objects.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The matches in the engine's available order.</returns>
        internal static T[] FindIncludingInactiveUnsorted<T>()
            where T : UnityEngine.Object
        {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#elif LEGATO_UNITY_6_OBJECT_QUERIES
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
#elif BEAT_SABER_1_29_0 || LEGATO_HISTORICAL_GAME
            if (!typeof(Component).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(GameObject))
            {
                return UnityEngine.Object.FindObjectsOfType<T>();
            }

            return UnityEngine.Resources.FindObjectsOfTypeAll<T>()
                .Where(value => (value.hideFlags & HideFlags.DontSave) == HideFlags.None &&
                    (value is Component component ? component.gameObject.scene.IsValid() :
                     value is GameObject gameObject && gameObject.scene.IsValid()))
                .OrderBy(value => value.GetInstanceID()).ToArray();
#else
            return UnityEngine.Object.FindObjectsOfType<T>(true);
#endif
        }

        /// <summary>
        /// Finds every loaded object, including assets and inactive scene objects.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The loaded objects.</returns>
        internal static T[] FindLoaded<T>()
            where T : UnityEngine.Object =>
            UnityEngine.Resources.FindObjectsOfTypeAll<T>();

        /// <summary>
        /// Finds active objects without requiring a specific match order.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The active matches.</returns>
        internal static T[] FindActiveUnsorted<T>()
            where T : UnityEngine.Object
        {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#elif LEGATO_UNITY_6_OBJECT_QUERIES
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }

        /// <summary>
        /// Finds active objects in instance ID order.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The active matches.</returns>
        internal static T[] FindActive<T>()
            where T : UnityEngine.Object
        {
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3 || LEGATO_UNITY_6_OBJECT_QUERIES
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }
    }
}
