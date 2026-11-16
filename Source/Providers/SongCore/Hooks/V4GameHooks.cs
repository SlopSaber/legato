#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Reflection;
using CustomJSONData.CustomBeatmap;
using MonoMod.RuntimeDetour;
using Zenject;

namespace SongCore.Hooks
{
    internal sealed class V4GameHooks : IInitializable, IDisposable
    {
        private Hook? _notes;
        private Hook? _obstacles;
        private Hook? _sliders;

        public void Initialize()
        {
            const BindingFlags methods = BindingFlags.NonPublic | BindingFlags.Instance;
            _notes = new Hook(typeof(BeatmapObjectSpawnController).GetMethod(nameof(BeatmapObjectSpawnController.HandleNoteDataCallback), methods)!, SpawnNote, true);
            _obstacles = new Hook(typeof(BeatmapObjectSpawnController).GetMethod(nameof(BeatmapObjectSpawnController.HandleObstacleDataCallback), methods)!, SpawnObstacle, true);
            _sliders = new Hook(typeof(BeatmapObjectSpawnController).GetMethod(nameof(BeatmapObjectSpawnController.HandleSliderDataCallback), methods)!, SpawnSlider, true);
        }

        public void Dispose()
        {
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
    }
}
#endif
