#if LEGATO_NOODLE_LEGACY_GAMEPLAY
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck;
using static Heck.HeckController;
using static NoodleExtensions.NoodleController;

namespace NoodleExtensions.HarmonyPatches.ObjectProcessing;

internal static class LegacyNoteRows
{
    internal static void Prepare(CustomBeatmapData chart)
    {
        float center = chart.beatmapLinesData.Length * 0.5f;
        foreach (IGrouping<float, CustomNoteData> row in chart.beatmapObjectDatas.OfType<CustomNoteData>().GroupBy(note => note.time))
        {
            var positions = row.Select(note =>
            {
                List<float?>? position = note.customData.GetNullableFloats(V2_POSITION)?.ToList();
                return (
                    Note: note,
                    X: position?.ElementAtOrDefault(0) + center ?? note.lineIndex,
                    Y: position?.ElementAtOrDefault(1) ?? (float)note.noteLineLayer,
                    Custom: position?.Any(value => value.HasValue) == true);
            }).ToArray();
            if (positions.Any(item => item.Custom))
            {
                foreach (var column in positions.GroupBy(item => item.X))
                {
                    int layer = 0;
                    foreach (var item in column.OrderBy(item => item.Y))
                    {
                        CustomData data = item.Note.customData;
                        data[INTERNAL_STARTNOTELINELAYER] = (float)layer++;
                        data[INTERNAL_FLIPLINEINDEX] = item.X;
                        data[INTERNAL_FLIPYSIDE] = 0f;
                    }
                }

                var basicNotes = positions.Where(item => item.Note.noteType.IsBasicNote()).ToArray();
                if (basicNotes.Length == 2 && basicNotes[0].Note.noteType != basicNotes[1].Note.noteType)
                {
                    var first = basicNotes[0];
                    var second = basicNotes[1];
                    if (first.Note.noteType == NoteType.NoteA ? first.X > second.X : first.X < second.X)
                    {
                        float side = first.X > second.X ? 1f : -1f;
                        if ((first.X - second.X) * (first.Y - second.Y) < 0)
                        {
                            side = -side;
                        }

                        first.Note.customData[INTERNAL_FLIPLINEINDEX] = second.X;
                        first.Note.customData[INTERNAL_FLIPYSIDE] = side;
                        second.Note.customData[INTERNAL_FLIPLINEINDEX] = first.X;
                        second.Note.customData[INTERNAL_FLIPYSIDE] = -side;
                    }
                }
            }

            foreach (var item in positions)
            {
                CustomData data = item.Note.customData;
                List<float?>? flip = data.GetNullableFloats(V2_FLIP)?.ToList();
                if (flip?.ElementAtOrDefault(0) is float x)
                {
                    data[INTERNAL_FLIPLINEINDEX] = x + center;
                }

                if (flip?.ElementAtOrDefault(1) is float y)
                {
                    data[INTERNAL_FLIPYSIDE] = y;
                }
            }
        }
    }
}
#endif
