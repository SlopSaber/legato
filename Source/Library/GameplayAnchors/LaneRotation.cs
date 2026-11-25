#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;
#if LEGATO_BEFORE_MULTIPLAYER
using NativeBeatmapData = global::BeatmapData;
#else
using NativeBeatmapData = global::IReadonlyBeatmapData;
#endif

namespace Legato.Gameplay
{
    internal sealed class LaneRotation
    {
        private readonly List<(float Time, Quaternion Rotation, string[] Tracks)> _samples = new();
        private readonly Dictionary<string, Func<string, Quaternion?>> _trackRotations = new();
        private readonly bool _leftHanded;

        internal LaneRotation(NativeBeatmapData data, bool leftHanded, DiContainer container)
        {
            _leftHanded = leftHanded;
            var properties = new Dictionary<Type, PropertyInfo?>();
#if LEGATO_BEFORE_MULTIPLAYER
            var notes = data.beatmapLinesData.SelectMany(line => line.beatmapObjectsData).OfType<NoteData>()
                .Where(note => note.noteType != NoteType.Bomb);
#else
            var notes = data.allBeatmapDataItems.OfType<NoteData>().Where(note => note.gameplayType != NoteData.GameplayType.Bomb);
#endif
            foreach (NoteData note in notes.OrderBy(note => note.time))
            {
                if (_samples.Count > 0 && Mathf.Approximately(_samples[_samples.Count - 1].Time, note.time))
                    continue;

                Type type = note.GetType();
                if (!properties.TryGetValue(type, out PropertyInfo? property))
                {
                    property = type.GetProperty("customData");
                    properties.Add(type, property);
                }

                var custom = property?.GetValue(note) as IDictionary<string, object>;
                _samples.Add((note.time, ReadRotation(custom), ReadTracks(custom)));
            }

            BindTracks(container);
        }

        internal Quaternion At(float time)
        {
            if (_samples.Count == 0) return Quaternion.identity;
            int low = 0, high = _samples.Count - 1, current = -1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (_samples[middle].Time <= time)
                {
                    current = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            if (current < 0) return Sample(_samples[0]);
            var from = _samples[current];
            Quaternion rotation = Sample(from);
            if (current == _samples.Count - 1) return rotation;
            var to = _samples[current + 1];
            if (from.Tracks.Length > 0 && ArrayEquals(from.Tracks, to.Tracks)) return rotation;
            return Quaternion.Slerp(rotation, Sample(to), Mathf.InverseLerp(from.Time, to.Time, time));
        }

        private void BindTracks(DiContainer container)
        {
            var names = _samples.SelectMany(sample => sample.Tracks).Distinct().ToArray();
            if (names.Length == 0) return;
            Type? track = Type.GetType("Heck.Animation.Track, Heck");
            Type? extensions = Type.GetType("Heck.Animation.AnimationExtensions, Heck");
            if (track == null || extensions == null) return;
            try
            {
                Type dictionary = typeof(Dictionary<,>).MakeGenericType(typeof(string), track);
                if (container.TryResolve(dictionary) is not IDictionary tracks) return;
                MethodInfo? read = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method => method.Name == "GetProperty" && method.IsGenericMethodDefinition &&
                        method.GetParameters().Length == 2)?.MakeGenericMethod(typeof(Quaternion));
                if (read == null) return;
                foreach (string name in names)
                    if (tracks.Contains(name) && tracks[name] is object instance &&
                        Delegate.CreateDelegate(typeof(Func<string, Quaternion?>), instance, read, false) is Func<string, Quaternion?> getter)
                        _trackRotations.Add(name, getter);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Gameplay anchor track rotation unavailable: {exception.Message}");
            }
        }

        private Quaternion ReadRotation(IDictionary<string, object>? custom)
        {
            object? value = Read(custom, "worldRotation", "_rotation");
            if (value == null) return Quaternion.identity;
            try
            {
                Quaternion rotation = value is IList angles && angles.Count >= 3
                    ? Quaternion.Euler(Convert.ToSingle(angles[0]), Convert.ToSingle(angles[1]), Convert.ToSingle(angles[2]))
                    : Quaternion.Euler(0f, Convert.ToSingle(value), 0f);
                return Mirror(rotation);
            }
            catch (Exception exception) when (exception is FormatException || exception is InvalidCastException || exception is OverflowException)
            {
                return Quaternion.identity;
            }
        }

        private static string[] ReadTracks(IDictionary<string, object>? custom) => Read(custom, "track", "_track") switch
        {
            string name => new[] { name },
            IEnumerable names => names.OfType<string>().ToArray(),
            _ => Array.Empty<string>(),
        };

        private static object? Read(IDictionary<string, object>? custom, string name, string legacy) =>
            custom != null && (custom.TryGetValue(name, out object value) || custom.TryGetValue(legacy, out value)) ? value : null;

        private Quaternion Sample((float Time, Quaternion Rotation, string[] Tracks) sample)
        {
            Quaternion rotation = sample.Rotation;
            foreach (string track in sample.Tracks)
                if (_trackRotations.TryGetValue(track, out var read))
                    rotation *= Mirror(read("offsetWorldRotation") ?? Quaternion.identity);
            return rotation;
        }

        private Quaternion Mirror(Quaternion rotation) =>
            _leftHanded ? new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w) : rotation;

        private static bool ArrayEquals(string[] first, string[] second)
        {
            if (first.Length != second.Length) return false;
            for (int index = 0; index < first.Length; index++)
                if (first[index] != second[index]) return false;
            return true;
        }
    }
}
