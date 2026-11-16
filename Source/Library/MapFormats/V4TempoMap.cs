#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Legato.Beatmaps.Formats
{
    internal sealed class V4TempoMap
    {
        internal readonly struct Region
        {
            internal readonly double StartBeat;
            internal readonly double EndBeat;
            internal readonly double StartSeconds;
            internal readonly double EndSeconds;
            internal double Bpm => 60 * (EndBeat - StartBeat) / (EndSeconds - StartSeconds);

            internal Region(double startBeat, double endBeat, double startSeconds, double endSeconds)
            {
                StartBeat = startBeat; EndBeat = endBeat; StartSeconds = startSeconds; EndSeconds = endSeconds;
            }
        }

        private readonly Region[] _regions;
        private readonly double _fallbackBpm;
        internal double Duration { get; }
        internal IReadOnlyList<Region> Regions => _regions;

        internal V4TempoMap(JObject audio, double fallbackBpm)
        {
            if (fallbackBpm <= 0 || double.IsNaN(fallbackBpm) || double.IsInfinity(fallbackBpm))
                throw new ArgumentOutOfRangeException(nameof(fallbackBpm));
            _fallbackBpm = fallbackBpm;
            double frequency = V4Documents.Number(audio["songFrequency"] ?? audio["_songFrequency"], 0, "songFrequency");
            double sampleCount = V4Documents.Number(audio["songSampleCount"] ?? audio["_songSampleCount"], 0, "songSampleCount");
            if (frequency <= 0 || sampleCount < 0) throw new System.IO.InvalidDataException("Invalid audio sample metadata.");
            Duration = sampleCount / frequency;
            bool legacy = audio["_regions"] != null;
            List<Region> regions = new();
            foreach (JObject source in V4Documents.Objects(audio, legacy ? "_regions" : "bpmData"))
            {
                double startBeat = V4Documents.Number(source[legacy ? "_startBeat" : "sb"], 0, "startBeat");
                double endBeat = V4Documents.Number(source[legacy ? "_endBeat" : "eb"], 0, "endBeat");
                double startSeconds = V4Documents.Number(source[legacy ? "_startSampleIndex" : "si"], 0, "startSampleIndex") / frequency;
                double endSeconds = V4Documents.Number(source[legacy ? "_endSampleIndex" : "ei"], 0, "endSampleIndex") / frequency;
                if (endBeat <= startBeat || endSeconds <= startSeconds)
                    throw new System.IO.InvalidDataException("Audio BPM regions must have positive duration and beat span.");
                regions.Add(new Region(startBeat, endBeat, startSeconds, endSeconds));
            }
            regions.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            for (int i = 1; i < regions.Count; i++)
                if (regions[i].StartBeat < regions[i - 1].EndBeat || regions[i].StartSeconds < regions[i - 1].EndSeconds)
                    throw new System.IO.InvalidDataException("Audio BPM regions overlap or run backwards.");
            _regions = regions.ToArray();
        }

        internal double BeatToSeconds(double beat)
        {
            if (_regions.Length == 0) return beat * 60 / _fallbackBpm;
            Region region = _regions[FindRegion(beat, false)];
            return region.StartSeconds + (beat - region.StartBeat) * 60 / region.Bpm;
        }

        internal double SecondsToBeat(double seconds)
        {
            if (_regions.Length == 0) return seconds * _fallbackBpm / 60;
            Region region = _regions[FindRegion(seconds, true)];
            return region.StartBeat + (seconds - region.StartSeconds) * region.Bpm / 60;
        }

        internal double BpmAtBeat(double beat) => _regions.Length == 0 ? _fallbackBpm : _regions[FindRegion(beat, false)].Bpm;

        private int FindRegion(double position, bool seconds)
        {
            int low = 0;
            int high = _regions.Length - 1;
            while (low < high)
            {
                int middle = low + (high - low + 1) / 2;
                double start = seconds ? _regions[middle].StartSeconds : _regions[middle].StartBeat;
                if (start <= position) low = middle;
                else high = middle - 1;
            }
            return low;
        }
    }
}
