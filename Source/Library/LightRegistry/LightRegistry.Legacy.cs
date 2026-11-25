#if LEGATO_CHROMA_LEGACY_LIGHTS
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Legato.Lighting
{
    internal static class LightRegistry
    {
        private static readonly ConditionalWeakTable<LightWithIdManager, Dictionary<int, LightGroup>> Groups = new();
        private static readonly FieldInfo ColorChanged = AccessTools.Field(typeof(LightWithIdManager), nameof(LightWithIdManager.didSetColorForIdEvent));

        internal static bool ContainsId(LightWithIdManager manager, int id) => id >= 0 && id < manager._lights.Length;

        internal static IReadOnlyList<LightWithId> GetLights(LightWithIdManager manager, int id)
        {
            return ContainsId(manager, id) ? GetGroup(manager, id).Snapshot : Array.Empty<LightWithId>();
        }

        internal static int IndexOf(LightWithIdManager manager, int id, LightWithId light)
        {
            return ContainsId(manager, id) ? GetGroup(manager, id).Slots.IndexOf(light) : -1;
        }

        internal static int Track(LightWithIdManager manager, LightWithId light)
        {
            LightGroup group = GetGroup(manager, light.id);
            int index = group.Slots.IndexOf(light);
            if (index >= 0)
            {
                return index;
            }

            group.Slots.Add(light);
            group.UpdateSnapshot();
            return group.Slots.Count - 1;
        }

        internal static void ClearSlot(LightWithIdManager manager, int id, int index)
        {
            LightGroup group = GetGroup(manager, id);
            group.Slots[index] = null!;
            group.UpdateSnapshot();
        }

        internal static void MarkColorChanged(LightWithIdManager manager)
        {
            manager._lastColorChangeFrameNum = Time.frameCount;
        }

        internal static void PublishGroupColor(LightWithIdManager manager, int id, Color color)
        {
            manager.colors[id] = color;
            MarkColorChanged(manager);
            ((Action<int, Color>?)ColorChanged.GetValue(manager))?.Invoke(id, color);
        }

        private static LightGroup GetGroup(LightWithIdManager manager, int id)
        {
            Dictionary<int, LightGroup> groups = Groups.GetOrCreateValue(manager);
            if (!groups.TryGetValue(id, out LightGroup group))
            {
                groups[id] = group = new LightGroup(manager._lights[id]);
            }

            return group;
        }

        private sealed class LightGroup
        {
            internal LightGroup(IEnumerable<LightWithId>? active)
            {
                this.Slots = new List<LightWithId>(active ?? Array.Empty<LightWithId>());
                this.Snapshot = this.Slots.ToArray();
            }

            internal List<LightWithId> Slots { get; }

            internal IReadOnlyList<LightWithId> Snapshot { get; private set; }

            internal void UpdateSnapshot() => this.Snapshot = this.Slots.ToArray();
        }
    }
}
#endif
