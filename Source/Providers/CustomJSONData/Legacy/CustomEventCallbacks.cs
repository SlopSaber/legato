using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
#if LEGATO_EARLY_STANDARD_MAPS
using FloatVariable = FloatSO;
#endif

namespace CustomJSONData
{
    [HarmonyPatch(typeof(BeatmapObjectCallbackController))]
    public static class CustomEventCallbacks
    {
        private static readonly ConditionalWeakTable<BeatmapObjectCallbackController, EventCursor> _cursors = new();

        public static event Action<BeatmapObjectCallbackController, CustomEventData>? CustomEventTriggered;

#if LEGATO_CUSTOM_JSON_EARLY_LOADER
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BeatmapObjectCallbackController.Init))]
        private static void Reset(BeatmapObjectCallbackController __instance) => _cursors.Remove(__instance);

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        private static void Update(BeatmapObjectCallbackController __instance, BeatmapDataModel ____beatmapDataModel, FloatVariable ____songTime)
        {
            if (____beatmapDataModel == null || ____beatmapDataModel.beatmapData is not CustomBeatmapData chart)
            {
                _cursors.Remove(__instance);
                return;
            }

            if (!_cursors.TryGetValue(__instance, out EventCursor cursor) || !ReferenceEquals(cursor.Events, chart.customEventDatas))
            {
                _cursors.Remove(__instance);
                cursor = new EventCursor(chart.customEventDatas);
                _cursors.Add(__instance, cursor);
            }

            TriggerEvents(__instance, cursor, ____songTime.value);
        }
#else
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

            TriggerEvents(__instance, cursor, ____audioTimeSource.songTime);
        }
#endif

        private static void TriggerEvents(BeatmapObjectCallbackController controller, EventCursor cursor, float songTime)
        {
            while (cursor.Index < cursor.Events.Count && cursor.Events[cursor.Index].time < songTime)
            {
                CustomEventData item = cursor.Events[cursor.Index++];
                CustomEventTriggered?.Invoke(controller, item);
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
