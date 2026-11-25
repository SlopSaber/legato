using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;

namespace CustomJSONData
{
    [HarmonyPatch(typeof(BeatmapObjectCallbackController))]
    public static class CustomEventCallbacks
    {
        private static readonly ConditionalWeakTable<BeatmapObjectCallbackController, EventCursor> _cursors = new();

        public static event Action<BeatmapObjectCallbackController, CustomEventData>? CustomEventTriggered;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BeatmapObjectCallbackController.SetNewBeatmapData))]
        private static void Reset(BeatmapObjectCallbackController __instance, BeatmapData beatmapData)
        {
            _cursors.Remove(__instance);
            if (beatmapData is CustomBeatmapData customMap && customMap.customEventDatas.Count > 0)
            {
                _cursors.Add(__instance, new EventCursor(customMap.customEventDatas));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        private static void Update(BeatmapObjectCallbackController __instance, IAudioTimeSource ____audioTimeSource)
        {
            if (!_cursors.TryGetValue(__instance, out EventCursor cursor))
            {
                return;
            }

            float songTime = ____audioTimeSource.songTime;
            while (cursor.Index < cursor.Events.Count && cursor.Events[cursor.Index].time < songTime)
            {
                CustomEventData item = cursor.Events[cursor.Index++];
                CustomEventTriggered?.Invoke(__instance, item);
            }
        }

        private sealed class EventCursor
        {
            internal EventCursor(IReadOnlyList<CustomEventData> events)
            {
                Events = events;
            }

            internal IReadOnlyList<CustomEventData> Events { get; }

            internal int Index { get; set; }
        }
    }
}
