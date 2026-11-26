namespace Legato.Gameplay {
    internal static class PracticePlayback {
        internal static float SpawnAheadTime(BeatmapObjectSpawnController controller) =>
#if !LEGATO_VARIABLE_MOVEMENT_DATA
            controller._beatmapObjectSpawnMovementData.spawnAheadTime;
#else
            controller._variableMovementDataProvider.spawnAheadTime;
#endif

#if LEGATO_BEFORE_MULTIPLAYER
        internal static void ApplyJumpSettings(BeatmapObjectSpawnController controller, float speed, float offset) {
            var movement = controller._beatmapObjectSpawnMovementData;
            movement._startNoteJumpMovementSpeed = speed;
            movement._noteJumpStartBeatOffset = offset;
            controller.EarlyEventsWereProcessed();
        }

        internal static void ResetCallbacks(BeatmapObjectCallbackController controller, BeatmapObjectSpawnController spawner, float songTime, float firstObjectTime) {
            var data = controller._beatmapData;
            if (data == null) return;

            controller._spawningStartTime = firstObjectTime;
            controller.SetNewBeatmapData(data);
            controller._beatmapObjectDataCallbackCacheList.Clear();
            var lines = data.beatmapLinesData;
            for (int line = 0; line < lines.Length; line++) {
                var objects = lines[line].beatmapObjectsData;
                int index = 0;
                while (index < objects.Length && objects[index].time < firstObjectTime) index++;
                foreach (var callback in controller._beatmapObjectCallbackData)
                    callback.nextObjectIndexInLine[line] = index;
            }

            var events = data.beatmapEventData;
            controller._nextEventIndex = EventIndex(events, songTime);
            spawner._spawnRotationProcesser = new SpawnRotationProcessor();
            spawner._variableBPMProcessor.SetBPM(spawner._initData.beatsPerMinute);
            for (int index = 0; index < controller._nextEventIndex; index++) {
                var item = events[index];
                if (item.type.IsEarlyEvent()) spawner.ProcessEarlyBeatmapEventData(item);
                else spawner.ProcessLateBeatmapEventData(item);
            }
            spawner.EarlyEventsWereProcessed();
            foreach (var callback in controller._beatmapEventCallbackData) {
                callback.nextEventIndex = ReferenceEquals(callback, spawner._beatmapEventCallbackData)
                    ? controller._nextEventIndex
                    : EventIndex(events, songTime + callback.aheadTime);
            }
        }

        private static int EventIndex(BeatmapEventData[] events, float songTime) {
            int index = 0;
            while (index < events.Length && events[index].time < songTime) index++;
            return index;
        }
#endif
    }
}
