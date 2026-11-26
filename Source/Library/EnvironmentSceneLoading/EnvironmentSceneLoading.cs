using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace Legato.Scenes {
    internal static class EnvironmentSceneLoading {
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
        private static readonly Dictionary<Scene, AsyncOperationHandle<SceneInstance>> Owned =
            new Dictionary<Scene, AsyncOperationHandle<SceneInstance>>();
#endif
        internal static void Load(MonoBehaviour host, string name, Action<Scene, Exception> completed) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var known = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++) known.Add(SceneManager.GetSceneAt(i).handle);
#if LEGATO_BEFORE_MULTIPLAYER
            // The environment decorator needs its bindings before native lighting components reach Start.
            bool delivered = false;
            void SceneLoaded(Scene scene, LoadSceneMode mode) {
                if (mode != LoadSceneMode.Additive || known.Contains(scene.handle) || scene.name != name) return;
                SceneManager.sceneLoaded -= SceneLoaded;
                delivered = true;
                completed(scene, null);
            }

            SceneManager.sceneLoaded += SceneLoaded;
            AsyncOperation operation;
            try {
                operation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                if (operation == null) throw new InvalidOperationException("Native scene load did not start: " + name);
            }
            catch {
                SceneManager.sceneLoaded -= SceneLoaded;
                throw;
            }

            operation.priority = int.MaxValue;
            operation.completed += _ => {
                SceneManager.sceneLoaded -= SceneLoaded;
                if (!delivered) completed(default, new InvalidOperationException("Native scene did not load: " + name));
            };
#else
            var operation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            if (operation == null) throw new InvalidOperationException("Native scene load did not start: " + name);
            operation.priority = int.MaxValue;
            operation.completed += _ => {
                for (int i = 0; i < SceneManager.sceneCount; i++) {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && !known.Contains(scene.handle) && scene.name == name) {
                        completed(scene, null);
                        return;
                    }
                }
                completed(default, new InvalidOperationException("Native scene did not load: " + name));
            };
#endif
#else
            var operation = Addressables.LoadSceneAsync(name, LoadSceneMode.Additive, true, int.MaxValue);
            host.StartCoroutine(operation);
            operation.Completed += result => {
                if (result.Status != AsyncOperationStatus.Succeeded) {
                    var error = result.OperationException ?? new InvalidOperationException("Addressable scene did not load: " + name);
                    Addressables.Release(result);
                    completed(default, error);
                    return;
                }
                Owned[result.Result.Scene] = result;
                completed(result.Result.Scene, null);
            };
#endif
        }

        internal static void Unload(Scene scene, Action completed = null) {
#if !BEAT_SABER_1_29_0 && !LEGATO_BEFORE_MULTIPLAYER
            if (Owned.TryGetValue(scene, out var handle)) {
                Owned.Remove(scene);
                var unloading = Addressables.UnloadSceneAsync(handle, true);
                if (completed != null) unloading.Completed += _ => completed();
                return;
            }
#endif
            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null) completed?.Invoke();
            else if (completed != null) operation.completed += _ => completed();
        }
    }
}
