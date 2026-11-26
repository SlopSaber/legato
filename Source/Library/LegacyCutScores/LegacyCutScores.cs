#if LEGATO_BEFORE_MULTIPLAYER
global using ColorType = Legato.Gameplay.LegacyNoteColor;

using System;
using System.Collections.Generic;

namespace Legato.Gameplay {
    public enum LegacyNoteColor { None = -1, ColorA, ColorB }

    internal readonly struct LegacyCutScore {
        internal readonly int Before;
        internal readonly int After;
        internal readonly int Center;

        internal LegacyCutScore(NoteCutInfo cut) {
            ScoreModel.RawScoreWithoutMultiplier(cut, out Before, out After, out Center);
        }
    }

    internal sealed class LegacyCutScores {
        private readonly Dictionary<SaberSwingRatingCounter, (NoteCutInfo Cut, Action<LegacyCutScore> Report)> _pending = new();

        internal static LegacyNoteColor Color(NoteData note) => note.noteType switch {
            NoteType.NoteA => LegacyNoteColor.ColorA,
            NoteType.NoteB => LegacyNoteColor.ColorB,
            _ => LegacyNoteColor.None
        };

        internal void Track(NoteCutInfo cut, Action<LegacyCutScore> report) {
            var swing = cut.swingRatingCounter;
            if (swing == null || swing.didFinish) {
                report(new LegacyCutScore(cut));
                return;
            }
            _pending.Add(swing, (cut, report));
            swing.didFinishEvent += Complete;
        }

        private void Complete(SaberSwingRatingCounter swing) {
            if (!_pending.TryGetValue(swing, out var pending)) return;
            swing.didFinishEvent -= Complete;
            _pending.Remove(swing);
            pending.Report(new LegacyCutScore(pending.Cut));
        }

        internal void Clear(bool reportPending) {
            foreach (var entry in _pending) {
                entry.Key.didFinishEvent -= Complete;
                if (reportPending) entry.Value.Report(new LegacyCutScore(entry.Value.Cut));
            }
            _pending.Clear();
        }
    }
}
#endif
