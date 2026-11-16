#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Legato.Lighting
{
    internal static class LightRegistry
    {
#if BEAT_SABER_1_29_0
        private static readonly ConditionalWeakTable<LightWithIdManager, Cache> Caches = new();

        private sealed class Cache
        {
            internal readonly Dictionary<int, LegacyLightList> Views = new();
        }

        private sealed class EmptyLight : ILightWithId
        {
            internal static readonly EmptyLight Instance = new();
            public bool isRegistered => false;
            public int lightId => -1;
            public void __SetIsRegistered() { }
            public void __SetIsUnRegistered() { }
            public void ColorWasSet(UnityEngine.Color color) { }
        }

        private sealed class LegacyLightList : IReadOnlyList<ILightWithId>
        {
            internal readonly List<ILightWithId> Source;
            internal readonly int OriginalCount;

            internal LegacyLightList(List<ILightWithId> source)
            {
                Source = source;
                OriginalCount = source.Count;
            }

            public int Count => Source.Count;
            public ILightWithId this[int index] => Source[index] is EmptyLight ? null! : Source[index];

            public IEnumerator<ILightWithId> GetEnumerator()
            {
                for (int i = 0; i < Source.Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static Cache CreateCache(LightWithIdManager manager) => new();
#endif

        internal static bool ContainsId(LightWithIdManager manager, int id) => id >= 0 && id <
#if BEAT_SABER_1_29_0
            manager._lights.Length;
#else
            manager._oldMapping.Length;
#endif

        internal static bool UsesLegacyId(ILightWithId light) =>
#if BEAT_SABER_1_29_0
            true;
#else
            light.groupId == -1 || light.elementId == -1;
#endif

        internal static IReadOnlyList<ILightWithId> GetLights(LightWithIdManager manager, int id)
        {
            if (!ContainsId(manager, id))
            {
                return Array.Empty<ILightWithId>();
            }
#if BEAT_SABER_1_29_0
            var source = manager._lights[id];
            if (source == null)
            {
                return Array.Empty<ILightWithId>();
            }

            var cache = Caches.GetValue(manager, CreateCache);
            if (cache.Views.TryGetValue(id, out var view) && ReferenceEquals(view.Source, source) && view.OriginalCount == source.Count)
            {
                return view;
            }

            view = new LegacyLightList(source);
            cache.Views[id] = view;
            return view;
#else
            return manager._oldMapping[id]?.lightInstances ?? Array.Empty<ILightWithId>();
#endif
        }

        internal static int IndexOf(LightWithIdManager manager, int id, ILightWithId light)
        {
            if (!ContainsId(manager, id))
            {
                return -1;
            }
#if BEAT_SABER_1_29_0
            return manager._lights[id]?.IndexOf(light) ?? -1;
#else
            return Array.IndexOf(manager._oldMapping[id]?.lightInstances ?? Array.Empty<ILightWithId>(), light);
#endif
        }

        internal static void ClearSlot(LightWithIdManager manager, int id, int index)
        {
#if BEAT_SABER_1_29_0
            // The native color loop dereferences every slot; a disabled sentinel keeps indices stable.
            manager._lights[id][index] = EmptyLight.Instance;
            Caches.GetValue(manager, CreateCache).Views.Remove(id);
#else
            manager._oldMapping[id].lightInstances[index] = null!;
#endif
        }
    }
}
