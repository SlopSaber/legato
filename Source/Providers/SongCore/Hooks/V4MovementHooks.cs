#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Zenject;

namespace SongCore.Hooks
{
    internal sealed class V4MovementHooks : IInitializable, IDisposable
    {
        private readonly V4RelativeNjs? _curve;
        private readonly BeatmapCallbacksController _callbacks;
        private readonly AudioTimeSyncController _audio;
        private readonly List<Hook> _hooks = new();
        private readonly ConditionalWeakTable<NoteMovement, NoteState> _notes = new();
        private readonly ConditionalWeakTable<ObstacleController, ObstacleState> _obstacles = new();
        private readonly ConditionalWeakTable<SliderController, SliderState> _sliders = new();
        private BeatmapObjectSpawnMovementData? _movement;
        private float _initialSpeed;
        private float _initialHalfJump;
        private float _spawnAhead;
        private BeatmapObjectSpawnMovementData.NoteJumpValueType _jumpType;
        private int _revision;

        private V4MovementHooks(IDifficultyBeatmap difficulty, BeatmapCallbacksController callbacks, AudioTimeSyncController audio)
        {
            _curve = V4LoadingHooks.RelativeSpeedFor(difficulty);
            _callbacks = callbacks;
            _audio = audio;
        }

        public void Initialize()
        {
            if (_curve == null || !_curve.HasEvents) return;
            Add(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.Init), (Action<Action<BeatmapObjectSpawnMovementData, int, float, float, BeatmapObjectSpawnMovementData.NoteJumpValueType, float, IJumpOffsetYProvider, Vector3, Vector3>, BeatmapObjectSpawnMovementData, int, float, float, BeatmapObjectSpawnMovementData.NoteJumpValueType, float, IJumpOffsetYProvider, Vector3, Vector3>)InitializeMovement);
            Add(typeof(BeatmapCallbacksController), nameof(BeatmapCallbacksController.ManualUpdate), (Action<Action<BeatmapCallbacksController, float>, BeatmapCallbacksController, float>)UpdateCallbacks);
            Add(typeof(NoteMovement), nameof(NoteMovement.Init), (Action<Action<NoteMovement, float, float, Vector3, Vector3, Vector3, float, float, float, float, float, bool, bool>, NoteMovement, float, float, Vector3, Vector3, Vector3, float, float, float, float, float, bool, bool>)InitializeNote);
            Add(typeof(NoteMovement), nameof(NoteMovement.ManualUpdate), (Action<Action<NoteMovement>, NoteMovement>)UpdateNote);
            Add(typeof(ObstacleController), nameof(ObstacleController.Init), (Action<Action<ObstacleController, ObstacleData, float, Vector3, Vector3, Vector3, float, float, float, float>, ObstacleController, ObstacleData, float, Vector3, Vector3, Vector3, float, float, float, float>)InitializeObstacle);
            Add(typeof(ObstacleController), nameof(ObstacleController.ManualUpdate), (Action<Action<ObstacleController>, ObstacleController>)UpdateObstacle);
            Add(typeof(SliderController), nameof(SliderController.Init), (Action<Action<SliderController, SliderController.LengthType, SliderData, float, Vector3, Vector3, Vector3, Vector3, float, float, float, float>, SliderController, SliderController.LengthType, SliderData, float, Vector3, Vector3, Vector3, Vector3, float, float, float, float>)InitializeSlider);
            Add(typeof(SliderController), nameof(SliderController.ManualUpdate), (Action<Action<SliderController>, SliderController>)UpdateSlider);
        }

        private void Add(Type type, string name, Delegate handler) => _hooks.Add(new Hook(type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, handler, true));

        public void Dispose()
        {
            for (int index = _hooks.Count - 1; index >= 0; index--) _hooks[index].Dispose();
            _hooks.Clear();
            _movement = null;
        }

        private void InitializeMovement(Action<BeatmapObjectSpawnMovementData, int, float, float, BeatmapObjectSpawnMovementData.NoteJumpValueType, float, IJumpOffsetYProvider, Vector3, Vector3> original,
            BeatmapObjectSpawnMovementData movement, int lines, float speed, float bpm, BeatmapObjectSpawnMovementData.NoteJumpValueType jumpType, float value, IJumpOffsetYProvider offset, Vector3 right, Vector3 forward)
        {
            original(movement, lines, speed, bpm, jumpType, value, offset, right, forward);
            _movement = movement;
            _initialSpeed = movement._noteJumpMovementSpeed;
            _initialHalfJump = movement._jumpDuration * 0.5f;
            _jumpType = jumpType;
            float closestToZero = ClampSpeed(_initialSpeed + (_initialSpeed < 0 ? _curve!.Maximum : _curve!.Minimum));
            _spawnAhead = movement._moveDuration + HalfJump(closestToZero);
            ApplySpeed(_audio.songTime, true);
        }

        private float ClampSpeed(float speed) => _initialSpeed < 0 ? Mathf.Min(speed, -0.01f) : Mathf.Max(speed, 0.01f);

        private float HalfJump(float speed) => (int)_jumpType == 1
            ? _initialHalfJump / Mathf.Min(Mathf.Abs(speed / _initialSpeed), 1f) : _initialHalfJump;

        private void UpdateCallbacks(Action<BeatmapCallbacksController, float> original, BeatmapCallbacksController callbacks, float songTime)
        {
            if (ReferenceEquals(callbacks, _callbacks)) ApplySpeed(songTime, false);
            original(callbacks, songTime);
        }

        private void ApplySpeed(float songTime, bool force)
        {
            if (_movement == null) return;
            float speed = ClampSpeed(_initialSpeed + _curve!.At(songTime));
            if (!force && Mathf.Approximately(speed, _movement._noteJumpMovementSpeed)) return;
            float duration = HalfJump(speed) * 2f;
            if (float.IsNaN(speed) || float.IsInfinity(speed) || duration <= 0 || float.IsInfinity(duration))
                throw new InvalidOperationException("The v4 NJS curve exceeds native movement limits.");
            _movement._noteJumpMovementSpeed = speed;
            _movement._jumpDuration = duration;
            _movement._jumpDistance = speed * duration;
            Vector3 halfDistance = _movement._forwardVec * (_movement._jumpDistance * 0.5f);
            _movement._moveEndPos = _movement._centerPos + halfDistance;
            _movement._jumpEndPos = _movement._centerPos - halfDistance;
            _movement._moveStartPos = _movement._moveEndPos + _movement._forwardVec * _movement._moveDistance;
            _movement._spawnAheadTime = _spawnAhead;
            _revision++;
        }

        private void InitializeNote(Action<NoteMovement, float, float, Vector3, Vector3, Vector3, float, float, float, float, float, bool, bool> original,
            NoteMovement note, float beatTime, float rotation, Vector3 moveStart, Vector3 moveEnd, Vector3 jumpEnd, float moveDuration, float jumpDuration, float gravity, float flip, float endRotation, bool towardsPlayer, bool randomRotation)
        {
            original(note, beatTime, rotation, moveStart, moveEnd, jumpEnd, moveDuration, jumpDuration, gravity, flip, endRotation, towardsPlayer, randomRotation);
            if (_movement == null) return;
            NoteState state = _notes.GetValue(note, static _ => new NoteState());
            state.MoveStartOffset = note._floorMovement._startPos - _movement._moveStartPos;
            state.MoveEndOffset = note._floorMovement._endPos - _movement._moveEndPos;
            state.JumpStartOffset = note._jump._startPos - _movement._moveEndPos;
            state.JumpEndOffset = note._jump._endPos - _movement._jumpEndPos;
            state.GravityBase = note._jump._gravity * jumpDuration * jumpDuration * 0.25f;
            state.Revision = _revision;
        }

        private void UpdateNote(Action<NoteMovement> original, NoteMovement note)
        {
            if (_movement != null && !note._jump._missedMarkReported && _notes.TryGetValue(note, out NoteState state) && state.Revision != _revision)
            {
                float half = _movement._jumpDuration * 0.5f;
                note._floorMovement._startPos = _movement._moveStartPos + state.MoveStartOffset;
                note._floorMovement._endPos = _movement._moveEndPos + state.MoveEndOffset;
                note._floorMovement._startTime = note._jump._beatTime - _movement._moveDuration - half;
                note._jump._startPos = _movement._moveEndPos + state.JumpStartOffset;
                note._jump._endPos = _movement._jumpEndPos + state.JumpEndOffset;
                note._jump._jumpDuration = _movement._jumpDuration;
                note._jump._moveVec = (note._jump._endPos - note._jump._startPos) / _movement._jumpDuration;
                note._jump._gravity = state.GravityBase / (half * half);
                note._jump._startVerticalVelocity = note._jump._gravity * half;
                state.Revision = _revision;
            }
            original(note);
        }

        private void InitializeObstacle(Action<ObstacleController, ObstacleData, float, Vector3, Vector3, Vector3, float, float, float, float> original,
            ObstacleController obstacle, ObstacleData data, float rotation, Vector3 start, Vector3 middle, Vector3 end, float moveDuration, float jumpDuration, float width, float height)
        {
            original(obstacle, data, rotation, start, middle, end, moveDuration, jumpDuration, width, height);
            if (_movement == null) return;
            ObstacleState state = _obstacles.GetValue(obstacle, static _ => new ObstacleState());
            state.StartOffset = obstacle._startPos - _movement._moveStartPos;
            state.MiddleOffset = obstacle._midPos - _movement._moveEndPos;
            state.EndOffset = obstacle._endPos - _movement._jumpEndPos;
            state.Revision = _revision;
        }

        private void UpdateObstacle(Action<ObstacleController> original, ObstacleController obstacle)
        {
            if (_movement != null && _obstacles.TryGetValue(obstacle, out ObstacleState state) && state.Revision != _revision)
            {
                if (!obstacle._passedAvoidedMarkReported)
                {
                    obstacle._startPos = _movement._moveStartPos + state.StartOffset;
                    obstacle._midPos = _movement._moveEndPos + state.MiddleOffset;
                    obstacle._endPos = _movement._jumpEndPos + state.EndOffset;
                    obstacle._move1Duration = _movement._moveDuration;
                    obstacle._move2Duration = _movement._jumpDuration;
                    obstacle._startTimeOffset = obstacle._obstacleData.time - _movement._moveDuration - _movement._jumpDuration * 0.5f;
                    obstacle._passedAvoidedMarkTime = _movement._moveDuration + _movement._jumpDuration * 0.5f + obstacle._obstacleDuration + 0.15f;
                    obstacle._finishMovementTime = _movement._moveDuration + _movement._jumpDuration + obstacle._obstacleDuration;
                }
                obstacle._length = Mathf.Abs(_movement._noteJumpMovementSpeed) * obstacle._obstacleDuration;
                obstacle._stretchableObstacle.SetSizeAndColor(obstacle._width * 0.98f, obstacle._height, obstacle._length, obstacle._color);
                obstacle._bounds = obstacle._stretchableObstacle.bounds;
                state.Revision = _revision;
            }
            original(obstacle);
        }

        private void InitializeSlider(Action<SliderController, SliderController.LengthType, SliderData, float, Vector3, Vector3, Vector3, Vector3, float, float, float, float> original,
            SliderController slider, SliderController.LengthType length, SliderData data, float rotation, Vector3 headStart, Vector3 tailStart, Vector3 headEnd, Vector3 tailEnd, float duration, float headGravity, float tailGravity, float scale)
        {
            original(slider, length, data, rotation, headStart, tailStart, headEnd, tailEnd, duration, headGravity, tailGravity, scale);
            if (_movement == null) return;
            SliderState state = _sliders.GetValue(slider, static _ => new SliderState());
            state.StartOffset = slider._sliderMovement._headNoteJumpStartPos - _movement._moveEndPos;
            state.EndOffset = slider._sliderMovement._headNoteJumpEndPos - _movement._jumpEndPos;
            state.HeadGravityBase = headGravity * duration * duration * 0.25f;
            state.TailGravityBase = tailGravity * duration * duration * 0.25f;
            state.Revision = _revision;
        }

        private void UpdateSlider(Action<SliderController> original, SliderController slider)
        {
            if (_movement != null && _sliders.TryGetValue(slider, out SliderState state) && state.Revision != _revision)
            {
                SliderMovement motion = slider._sliderMovement;
                float half = _movement._jumpDuration * 0.5f;
                motion._headNoteJumpStartPos = _movement._moveEndPos + state.StartOffset;
                motion._headNoteJumpEndPos = _movement._jumpEndPos + state.EndOffset;
                motion._jumpDuration = _movement._jumpDuration;
                motion._headNoteGravity = state.HeadGravityBase / (half * half);
                motion._tailNoteGravity = state.TailGravityBase / (half * half);
                slider._sliderIntensityEffect._halfJumpDuration = half;
                slider._jumpDistance = _movement._jumpDistance;
                slider._zDistanceBetweenNotes = slider._sliderDuration * _movement._noteJumpMovementSpeed;
                SliderShaderHelper.SetInitialProperties(slider._materialPropertyBlockController.materialPropertyBlock, slider, _movement._noteJumpMovementSpeed);
                state.Revision = _revision;
            }
            original(slider);
        }

        private sealed class NoteState
        {
            internal Vector3 MoveStartOffset, MoveEndOffset, JumpStartOffset, JumpEndOffset;
            internal float GravityBase;
            internal int Revision;
        }
        private sealed class ObstacleState
        {
            internal Vector3 StartOffset, MiddleOffset, EndOffset;
            internal int Revision;
        }
        private sealed class SliderState
        {
            internal Vector3 StartOffset, EndOffset;
            internal float HeadGravityBase, TailGravityBase;
            internal int Revision;
        }
    }
}
#endif
