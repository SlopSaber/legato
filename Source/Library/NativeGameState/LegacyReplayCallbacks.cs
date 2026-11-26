#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Linq;

namespace Legato.Gameplay {
    internal static class LegacyReplayCallbacks {
        internal static bool IsReady(BeatmapObjectCallbackController callbacks, BeatmapObjectSpawnController spawner) =>
            callbacks._beatmapData != null && spawner._beatmapCallbackItemDataList != null;

        internal static void DespawnObjects(BeatmapObjectManager objects) {
            foreach (var note in objects._noteAPool.activeItems.ToArray()) objects.Despawn(note);
            foreach (var note in objects._noteBPool.activeItems.ToArray()) objects.Despawn(note);
            foreach (var note in objects._bombNotePool.activeItems.ToArray()) objects.Despawn(note);
            foreach (var obstacle in objects._obstaclePool.activeItems.ToArray()) objects.Despawn(obstacle);
        }

        internal static void ClearCutSounds(NoteCutSoundEffectManager sounds) {
            foreach (var sound in sounds._noteCutSoundEffectPool.activeItems.ToArray()) sound.StopPlayingAndFinish();
            sounds._prevNoteATime = -1f;
            sounds._prevNoteBTime = -1f;
        }

        internal static void ResetSoundCursor(BeatmapObjectCallbackController callbacks, NoteCutSoundEffectManager sounds, float time) {
            if (callbacks._beatmapData != null && sounds._beatmapObjectCallbackData != null) {
                SetObjectCursor(callbacks._beatmapData, sounds._beatmapObjectCallbackData, time);
            }
        }

        internal static void Seek(BeatmapObjectCallbackController callbacks, BeatmapObjectSpawnController spawner,
            float time, Func<NoteData, bool> shouldRespawnNote) {
            var data = callbacks._beatmapData;
            var seekStart = FindEarliestVisibleTime(data, spawner, time);
            callbacks.SetNewBeatmapData(data);
            var objectCallbacks = spawner._beatmapObjectCallbackData;
            foreach (var item in callbacks._beatmapObjectCallbackData) {
                SetObjectCursor(data, item, ReferenceEquals(item, objectCallbacks) ? seekStart : time);
            }

            var spawnEvents = spawner._beatmapEventCallbackData;
            spawner._spawnRotationProcesser = new SpawnRotationProcessor();
            spawner._variableBPMProcessor.SetBPM(spawner._initData.beatsPerMinute);
            foreach (var callback in callbacks._beatmapEventCallbackData) callback.nextEventIndex = 0;
            var events = data.beatmapEventData;
            while (spawnEvents.nextEventIndex < events.Length && events[spawnEvents.nextEventIndex].time < seekStart) {
                var item = events[spawnEvents.nextEventIndex++];
                if (item.type.IsEarlyEvent()) spawner.ProcessEarlyBeatmapEventData(item);
                else if (item.type.IsLateRotationEvent()) spawner.ProcessLateBeatmapEventData(item);
            }
            spawner.EarlyEventsWereProcessed();

            var batch = spawner._beatmapCallbackItemDataList;
            var spawnNote = batch.spawnNoteCallback;
            var spawnObstacle = batch.spawnObstacleCallback;
            var movement = spawner._beatmapObjectSpawnMovementData;
            try {
                batch.spawnNoteCallback = (note, angle) => {
                    if (note.time + movement.jumpDuration * 0.5f > time && shouldRespawnNote(note)) spawnNote(note, angle);
                };
                batch.spawnObstacleCallback = obstacle => {
                    if (obstacle.time + obstacle.duration + movement.jumpDuration * 0.5f > time) spawnObstacle(obstacle);
                };
                callbacks.LateUpdate();
            } finally {
                batch.spawnNoteCallback = spawnNote;
                batch.spawnObstacleCallback = spawnObstacle;
            }
        }

        private static float FindEarliestVisibleTime(BeatmapData data, BeatmapObjectSpawnController spawner, float time) {
            var minimumBpm = spawner._initData.beatsPerMinute;
            foreach (var item in data.beatmapEventData) {
                if (item.time > time) break;
                if (item.type.IsBPMChangeEvent() && item.value > 0) minimumBpm = Math.Min(minimumBpm, item.value);
            }
            var movement = spawner._beatmapObjectSpawnMovementData;
            var maximumHalfJump = Math.Max(1f, movement._startHalfJumpDurationInBeats + movement._noteJumpStartBeatOffset)
                * 60f / minimumBpm;
            var earliestTime = time - maximumHalfJump;
            var start = earliestTime;
            foreach (var line in data.beatmapLinesData) {
                foreach (var item in line.beatmapObjectsData) {
                    if (item.time >= earliestTime) break;
                    if (item is ObstacleData obstacle && obstacle.time + obstacle.duration >= earliestTime) {
                        start = Math.Min(start, obstacle.time);
                    }
                }
            }
            return start;
        }

        private static void SetObjectCursor(BeatmapData data, BeatmapObjectCallbackController.BeatmapObjectCallbackData callback, float time) {
            for (var lane = 0; lane < data.beatmapLinesData.Length; lane++) {
                var objects = data.beatmapLinesData[lane].beatmapObjectsData;
                int first = 0, end = objects.Length;
                while (first < end) {
                    var middle = first + (end - first) / 2;
                    if (objects[middle].time < time) first = middle + 1;
                    else end = middle;
                }
                callback.nextObjectIndexInLine[lane] = first;
            }
        }
    }
}
#endif
