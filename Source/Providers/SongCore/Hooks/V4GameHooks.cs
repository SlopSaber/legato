#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Reflection;
using CustomJSONData.CustomBeatmap;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Zenject;

namespace SongCore.Hooks
{
    internal sealed class V4GameHooks : IInitializable, IDisposable
    {
        private Hook? _notes;
        private Hook? _obstacles;
        private Hook? _sliders;
        private ILHook? _sliderMesh;

        public void Initialize()
        {
            const BindingFlags methods = BindingFlags.NonPublic | BindingFlags.Instance;
            _notes = new Hook(typeof(BeatmapObjectSpawnController).GetMethod(nameof(BeatmapObjectSpawnController.HandleNoteDataCallback), methods)!, SpawnNote, true);
            _obstacles = new Hook(typeof(BeatmapObjectSpawnController).GetMethod(nameof(BeatmapObjectSpawnController.HandleObstacleDataCallback), methods)!, SpawnObstacle, true);
            _sliders = new Hook(typeof(BeatmapObjectSpawnController).GetMethod(nameof(BeatmapObjectSpawnController.HandleSliderDataCallback), methods)!, SpawnSlider, true);
            _sliderMesh = new ILHook(typeof(SliderMeshController).GetMethod(nameof(SliderMeshController.CreateBezierPathAndMesh))!, context =>
            {
                var cursor = new ILCursor(context);
                cursor.GotoNext(MoveType.Before, instruction => instruction.MatchCallvirt<PathsHolder>(nameof(PathsHolder.UpdateVertexPathByBezierPath)));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_S, context.Method.Parameters[3]);
                cursor.EmitDelegate<Action<SliderMeshController, SliderData, float>>(RotateSliderPath);
            }, true);
        }

        public void Dispose()
        {
            _sliderMesh?.Dispose();
            _sliders?.Dispose();
            _obstacles?.Dispose();
            _notes?.Dispose();
        }

        private void SpawnNote(Action<BeatmapObjectSpawnController, NoteData> original, BeatmapObjectSpawnController controller, NoteData note) => Spawn(original, controller, note);
        private void SpawnObstacle(Action<BeatmapObjectSpawnController, ObstacleData> original, BeatmapObjectSpawnController controller, ObstacleData obstacle) => Spawn(original, controller, obstacle);
        private void SpawnSlider(Action<BeatmapObjectSpawnController, SliderData> original, BeatmapObjectSpawnController controller, SliderData slider) => Spawn(original, controller, slider);

        private static void Spawn<T>(Action<BeatmapObjectSpawnController, T> original, BeatmapObjectSpawnController controller, T data) where T : BeatmapObjectData
        {
            float previous = controller._spawnRotation;
            try
            {
                if (data is ICustomData custom && custom.customData.TryGetValue("legatoV4Rotation", out object? rotation))
                {
                    if (rotation is double number) controller._spawnRotation = (float)number;
                    else if (rotation is long integer) controller._spawnRotation = integer;
                    else if (rotation is float single) controller._spawnRotation = single;
                    else if (rotation is int small) controller._spawnRotation = small;
                }
                original(controller, data);
            }
            finally { controller._spawnRotation = previous; }
        }

        private static void RotateSliderPath(SliderMeshController mesh, SliderData data, float speed)
        {
            if (data is not ICustomData custom) return;
            float? tail = custom.customData.Get<float?>("legatoV4TailRotation");
            if (!tail.HasValue) return;
            float head = custom.customData.Get<float?>("legatoV4Rotation") ?? 0;
            float delta = Mathf.DeltaAngle(head, tail.Value);
            if (Mathf.Approximately(delta, 0)) return;
            BezierPath path = mesh.pathsHolder.bezierPath;
            float length = (data.tailTime - data.time) * speed;
            Quaternion tailRotation = Quaternion.Euler(0, delta, 0);
            Quaternion middleRotation = Quaternion.Euler(0, delta * 0.5f, 0);
            for (int index = 2; index < path.pointsCount; index++)
            {
                bool atTail = index >= path.pointsCount - 2;
                float along = atTail ? length : length * 0.5f;
                Vector3 origin = Vector3.forward * along;
                Quaternion rotation = atTail ? tailRotation : middleRotation;
                path.SetPoint(index, rotation * (path[index] - origin) + origin, true);
            }
        }
    }
}
#endif
