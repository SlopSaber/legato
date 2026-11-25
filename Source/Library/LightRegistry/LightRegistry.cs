#if !LEGATO_CHROMA_LEGACY_LIGHTS
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Legato.Lighting
{
    internal static class LightRegistry
    {
#if !LEGATO_GROUPED_LIGHT_IDS
        private static readonly ConditionalWeakTable<LightWithIdManager, Dictionary<int, LegacyLightList>> Caches = new();
#endif

        internal static bool ContainsId(LightWithIdManager manager, int id) => id >= 0 && id <
#if !LEGATO_GROUPED_LIGHT_IDS
            manager._lights.Length;
#else
            manager._oldMapping.Length;
#endif

        internal static bool UsesLegacyId(ILightWithId light) =>
#if !LEGATO_GROUPED_LIGHT_IDS
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
#if !LEGATO_GROUPED_LIGHT_IDS
            var source = manager._lights[id];
            if (source == null)
            {
                return Array.Empty<ILightWithId>();
            }

            var cache = Caches.GetOrCreateValue(manager);
            if (cache.TryGetValue(id, out var view) && ReferenceEquals(view.Source, source) && view.OriginalCount == source.Count)
            {
                return view;
            }

            view = new LegacyLightList(source);
            cache[id] = view;
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
#if !LEGATO_GROUPED_LIGHT_IDS
            return manager._lights[id]?.IndexOf(light) ?? -1;
#else
            return Array.IndexOf(manager._oldMapping[id]?.lightInstances ?? Array.Empty<ILightWithId>(), light);
#endif
        }

        internal static void ClearSlot(LightWithIdManager manager, int id, int index)
        {
#if !LEGATO_GROUPED_LIGHT_IDS
            // The native color loop dereferences every slot; a disabled sentinel keeps indices stable.
            manager._lights[id][index] = EmptyLight.Instance;
            Caches.GetOrCreateValue(manager).Remove(id);
#else
            manager._oldMapping[id].lightInstances[index] = null!;
#endif
        }

#if !LEGATO_GROUPED_LIGHT_IDS
        private sealed class EmptyLight : ILightWithId
        {
            public static EmptyLight Instance { get; } = new();

            public bool isRegistered => false;

            public int lightId => -1;

            public void __SetIsRegistered()
            {
            }

            public void __SetIsUnRegistered()
            {
            }

            public void ColorWasSet(UnityEngine.Color color)
            {
            }
        }

        private sealed class LegacyLightList : IReadOnlyList<ILightWithId>
        {
            internal LegacyLightList(List<ILightWithId> source)
            {
                this.Source = source;
                this.OriginalCount = source.Count;
            }

            public int Count => this.Source.Count;

            internal List<ILightWithId> Source { get; }

            internal int OriginalCount { get; }

            public ILightWithId this[int index] => this.Source[index] is EmptyLight ? null! : this.Source[index];

            public IEnumerator<ILightWithId> GetEnumerator()
            {
                for (int i = 0; i < this.Source.Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
#endif
    }
}
#endif
