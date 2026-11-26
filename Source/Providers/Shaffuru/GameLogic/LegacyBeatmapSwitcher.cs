#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Shaffuru.HarmonyPatches;
using UnityEngine;

namespace Shaffuru.GameLogic {
    class BeatmapSwitcher : IDisposable {
        static readonly System.Reflection.MethodInfo CloneEvent = AccessTools.Method(typeof(object), "MemberwiseClone");
        static readonly System.Reflection.FieldInfo EventTime = AccessTools.Field(typeof(BeatmapEventData), nameof(BeatmapEventData.time));

        readonly GameplayCoreSceneSetupData scene;
        readonly BeatmapObjectSpawnController spawnController;
        readonly BeatmapObjectCallbackController callbacks;
        readonly AudioTimeWrapper audioTime;
        readonly GameEnergyCounter energy;
        readonly RamCleaner ramCleaner;
        Coroutine switching;
        bool? savedNoFail;

        public readonly CustomAudioSource customAudioSource;

        public BeatmapSwitcher(GameplayCoreSceneSetupData scene, BeatmapObjectSpawnController spawnController,
            BeatmapObjectCallbackController callbacks, AudioTimeWrapper audioTime, GameEnergyCounter energy,
            CustomAudioSource customAudioSource, RamCleaner ramCleaner) {
            this.scene = scene;
            this.spawnController = spawnController;
            this.callbacks = callbacks;
            this.audioTime = audioTime;
            this.energy = energy;
            this.customAudioSource = customAudioSource;
            this.ramCleaner = ramCleaner;
        }

        public void SwitchToDifferentBeatmap(BeatmapLevel level, BeatmapKey difficulty, BeatmapData data, AudioClip clip,
            float startTime, float secondsToInsert = 0) {
            if (switching != null) SharedCoroutineStarter.instance.StopCoroutine(switching);
            RestoreGameplay();
            switching = SharedCoroutineStarter.instance.StartCoroutine(Switch(level, difficulty, data, clip, startTime, secondsToInsert));
        }

        IEnumerator Switch(BeatmapLevel level, BeatmapKey difficulty, BeatmapData data, AudioClip clip, float startTime, float secondsToInsert) {
            float reactionTime = Config.Instance.transition_reactionTime;
            float dissolveTime = reactionTime * 0.6f;
            spawnController._disableSpawning = true;
            HeckOffCutSoundsCrash.enablePatch = true;
            try {
                BeatmapObjectDissolver.DissolveAllAndEverything(dissolveTime);
                if (!ramCleaner.TrySkip() && audioTime.songLength - audioTime.songTime >= 30f) {
                    yield return new WaitForSecondsRealtime(dissolveTime * 0.8f);
                    audioTime.Pause();
                    customAudioSource.SetClip(clip);
                    yield return new WaitForSecondsRealtime(dissolveTime * 0.2f);
                    yield return ramCleaner.ClearRam();
                    for (int frame = 0; frame < 20; frame++) yield return null;
                    if (audioTime.isValid) audioTime.Resume();
                } else {
                    yield return null;
                }

                if (!audioTime.isValid) yield break;
                float timelineStart = audioTime.songTime;
                float chartStart = startTime - level.songTimeOffset;
                float cutoff = secondsToInsert > 0f ? secondsToInsert + 1f : float.PositiveInfinity;
                var replacement = CreateSegment(data, chartStart, timelineStart, reactionTime, cutoff);
                UpdateMovement(level, difficulty, data, chartStart);
                callbacks.SetNewBeatmapData(replacement);
                foreach (var callback in callbacks._beatmapEventCallbackData) callback.nextEventIndex = 0;
                spawnController._disableSpawning = false;

                float switchDelay = Mathf.Min(reactionTime * 0.5f, startTime);
                yield return new WaitUntil(() => !audioTime.isValid || audioTime.songTime >= timelineStart + switchDelay);
                if (!audioTime.isValid) yield break;
                customAudioSource.SetClip(clip);
                customAudioSource.Play(startTime + audioTime.songTime - timelineStart);
                HeckOffCutSoundsCrash.enablePatch = false;

                if (Config.Instance.transition_gracePeriod > 0f) {
                    yield return new WaitUntil(() => !audioTime.isValid || audioTime.songTime >= timelineStart + reactionTime);
                    if (!audioTime.isValid) yield break;
                    savedNoFail = energy.noFail;
                    energy.noFail = true;
                    yield return new WaitForSeconds(Config.Instance.transition_gracePeriod);
                }
            } finally {
                RestoreGameplay();
                switching = null;
            }
        }

        void UpdateMovement(BeatmapLevel level, BeatmapKey difficulty, BeatmapData data, float chartStart) {
            var movement = spawnController._beatmapObjectSpawnMovementData;
            float njs = difficulty.noteJumpMovementSpeed;
            if (njs <= 0f || scene.gameplayModifiers.fastNotes) njs = BeatmapDifficultyMethods.NoteJumpMovementSpeed(difficulty.difficulty);
            float bpm = level.beatsPerMinute;
            foreach (var item in data.beatmapEventData) {
                if (item.time > chartStart) break;
                if (item.type.IsBPMChangeEvent() && item.value > 0) bpm = item.value;
            }
            spawnController._variableBPMProcessor.SetBPM(bpm);
            var origin = spawnController.transform;
            movement.Init(data.beatmapLinesData.Length, njs, bpm, difficulty.noteJumpStartBeatOffset,
                movement._jumpOffsetY, origin.position, origin.right, origin.forward);
            spawnController._beatmapObjectCallbackData.aheadTime = movement.spawnAheadTime;
            spawnController._beatmapEventCallbackData.aheadTime = movement.spawnAheadTime;
        }

        static BeatmapData CreateSegment(BeatmapData data, float start, float timeline, float reaction, float cutoff) {
            var lines = new BeatmapLineData[data.beatmapLinesData.Length];
            for (int lane = 0; lane < lines.Length; lane++) {
                var objects = new List<BeatmapObjectData>();
                foreach (var item in data.beatmapLinesData[lane].beatmapObjectsData) {
                    float relative = item.time - start;
                    if (relative > cutoff) break;
                    if (relative < reaction) {
                        if (!(item is ObstacleData wall) || relative + wall.duration <= reaction) continue;
                        var copy = (ObstacleData)wall.GetCopy();
                        copy.UpdateDuration(relative + wall.duration - reaction);
                        copy.time = timeline + reaction;
                        objects.Add(copy);
                    } else {
                        var copy = item.GetCopy();
                        copy.time = timeline + relative;
                        objects.Add(copy);
                    }
                }
                lines[lane] = new BeatmapLineData { beatmapObjectsData = objects.ToArray() };
            }

            var previous = new Dictionary<BeatmapEventType, BeatmapEventData>();
            var events = new List<BeatmapEventData>();
            foreach (var item in data.beatmapEventData) {
                float relative = item.time - start;
                if (relative > cutoff) break;
                if (relative < 0f) {
                    if (!item.type.IsRotationEvent()) previous[item.type] = item;
                } else {
                    events.Add(MoveEvent(item, timeline + relative));
                }
            }
            events.InsertRange(0, previous.Values.OrderBy(item => item.time).Select(item => MoveEvent(item, timeline)));
            return new BeatmapData(lines, events.ToArray());
        }

        static BeatmapEventData MoveEvent(BeatmapEventData item, float time) {
            if (item.GetType() == typeof(BeatmapEventData)) return new BeatmapEventData(time, item.type, item.value);
            var copy = (BeatmapEventData)CloneEvent.Invoke(item, null);
            EventTime.SetValue(copy, time);
            return copy;
        }

        void RestoreGameplay() {
            HeckOffCutSoundsCrash.enablePatch = false;
            if (spawnController != null) spawnController._disableSpawning = false;
            if (energy != null && savedNoFail.HasValue) energy.noFail = savedNoFail.Value;
            savedNoFail = null;
        }

        public void Dispose() {
            if (switching != null && SharedCoroutineStarter.instance != null)
                SharedCoroutineStarter.instance.StopCoroutine(switching);
            switching = null;
            RestoreGameplay();
        }
    }
}
#endif
