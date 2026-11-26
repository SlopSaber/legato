#if LEGATO_BEFORE_MULTIPLAYER
namespace Legato.Gameplay {
    internal sealed class LegacyReplayScoreCounter {
        private int _multiplier = 1;
        private int _progress;

        internal int Score { get; private set; }

        internal void ApplyCut(NoteCutInfo cutInfo) {
            if (_multiplier < ScoreModel.kMaxMultiplier && ++_progress >= _multiplier * 2) {
                _multiplier *= 2;
                _progress = 0;
            }

            ScoreModel.RawScoreWithoutMultiplier(cutInfo, out var before, out var after, out var distance);
            Score += (before + after + distance) * _multiplier;
        }

        internal void BreakCombo() {
            _progress = 0;
            if (_multiplier > 1) _multiplier /= 2;
        }
    }
}
#endif
