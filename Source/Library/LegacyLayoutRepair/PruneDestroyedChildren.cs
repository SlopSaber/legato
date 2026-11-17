#if BEAT_SABER_1_29_0
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Legato.UI {
    [HarmonyPatch(typeof(HorizontalOrVerticalLayoutGroup), "SetChildrenAlongAxis")]
    internal static class PruneDestroyedChildren {
        private static readonly AccessTools.FieldRef<LayoutGroup, List<RectTransform>> Children =
            AccessTools.FieldRefAccess<LayoutGroup, List<RectTransform>>("m_RectChildren");

        [HarmonyPrefix]
        private static void Prefix(HorizontalOrVerticalLayoutGroup __instance) {
            var children = Children(__instance);
            var removed = 0;
            // A child can be destroyed after layout input calculation cached this list.
            for (var index = children.Count - 1; index >= 0; index--) {
                if (children[index] != null) continue;
                children.RemoveAt(index);
                removed++;
            }
            if (removed == 0) return;

            var path = __instance.name;
            for (var parent = __instance.transform.parent; parent != null; parent = parent.parent) {
                path = parent.name + "/" + path;
            }
            Debug.Log($"[Legato] Removed {removed} destroyed UI layout children from {path} before positioning.");
        }
    }
}
#endif
