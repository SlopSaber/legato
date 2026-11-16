#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Linq;
using Legato.Beatmaps.Formats;
using UnityEngine;

namespace SongCore.Hooks
{
    internal sealed class V4RelativeNjs
    {
        private readonly Point[] _points;
        internal float Minimum { get; }
        internal float Maximum { get; }
        internal bool HasEvents => _points.Length > 0;

        internal V4RelativeNjs(V4LegacyProjection projection)
        {
            _points = V4Documents.Objects(projection.Gameplay, "njsEvents")
                .Select((source, order) => new { Source = source, Order = order, Beat = V4Documents.Number(source["b"], 0, "njsEvents.b") })
                .OrderBy(value => value.Beat).ThenBy(value => value.Order)
                .Select(value =>
                {
                    var data = V4Documents.Object(value.Source["data"], "njsEvents.data");
                    double ease = V4Documents.Number(data["e"], 0, "njsEvents.e");
                    if (Math.Truncate(ease) != ease || ease < int.MinValue || ease > int.MaxValue)
                        throw new System.IO.InvalidDataException("Invalid variable NJS easing value.");
                    return new Point(
                        Float(projection.Tempo.BeatToSeconds(value.Beat)),
                        Float(V4Documents.Number(data["d"], 0, "njsEvents.d")),
                        V4Documents.Number(data["p"], 0, "njsEvents.p") != 0,
                        BeatmapDataLoader.ConvertEaseType((BeatmapSaveDataVersion3.BeatmapSaveData.EaseType)(int)ease));
                }).ToArray();
            float previous = 0, minimum = 0, maximum = 0;
            for (int index = 0; index < _points.Length; index++)
            {
                Point point = _points[index];
                if (point.Previous) point = new Point(point.Time, previous, true, point.Ease);
                _points[index] = point;
                previous = point.Delta;
                minimum = Mathf.Min(minimum, previous);
                maximum = Mathf.Max(maximum, previous);
            }
            Minimum = minimum;
            Maximum = maximum;
        }

        internal float At(float songTime)
        {
            if (_points.Length == 0 || songTime < _points[0].Time) return 0;
            int low = 0, high = _points.Length - 1;
            while (low < high)
            {
                int middle = low + (high - low + 1) / 2;
                if (_points[middle].Time <= songTime) low = middle;
                else high = middle - 1;
            }
            Point from = _points[low];
            if (low + 1 == _points.Length) return from.Delta;
            Point to = _points[low + 1];
            if (to.Ease == EaseType.None || to.Time <= from.Time) return from.Delta;
            float progress = Mathf.Clamp01((songTime - from.Time) / (to.Time - from.Time));
            return from.Delta + (to.Delta - from.Delta) * Interpolation.Interpolate(progress, to.Ease);
        }

        private static float Float(double value)
        {
            float result = (float)value;
            if (float.IsNaN(result) || float.IsInfinity(result))
                throw new System.IO.InvalidDataException("Variable NJS exceeds native numeric limits.");
            return result;
        }

        private readonly struct Point
        {
            internal readonly float Time;
            internal readonly float Delta;
            internal readonly bool Previous;
            internal readonly EaseType Ease;
            internal Point(float time, float delta, bool previous, EaseType ease)
            { Time = time; Delta = delta; Previous = previous; Ease = ease; }
        }
    }
}
#endif
