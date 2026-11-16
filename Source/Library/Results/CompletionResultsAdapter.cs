#nullable enable

using IPA.Utilities;

namespace Legato.Gameplay {
    internal static class CompletionResultsAdapter {
#if BEAT_SABER_1_45_3
        private static readonly FieldAccessor<LevelCompletionResultsBase, LevelScore>.Accessor ScoreAccessor =
            FieldAccessor<LevelCompletionResultsBase, LevelScore>.GetAccessor("score");
#else
        private static readonly FieldAccessor<LevelCompletionResults, int>.Accessor MultipliedScoreAccessor =
            FieldAccessor<LevelCompletionResults, int>.GetAccessor("multipliedScore");
#endif

        internal static void NegateMultipliedScore(ref LevelCompletionResults results) {
#if BEAT_SABER_1_45_3
            LevelCompletionResultsBase baseResults = results;
            var score = baseResults.score;
            ScoreAccessor(ref baseResults) = new LevelScore(-score.multiplied, score.modified, score.maxMultiplied, score.maxModified);
#else
            MultipliedScoreAccessor(ref results) = -results.multipliedScore;
#endif
        }

        internal static int GetModifiedScore(LevelCompletionResults results) {
#if BEAT_SABER_1_45_3
            return results.score.modified;
#else
            return results.modifiedScore;
#endif
        }

        internal static int GetMultipliedScore(LevelCompletionResults results) {
#if BEAT_SABER_1_45_3
            return results.score.multiplied;
#else
            return results.multipliedScore;
#endif
        }

        internal static int GetMissedCount(LevelCompletionResults results) {
#if BEAT_SABER_1_45_3
            return results.missedCutsCount;
#else
            return results.missedCount;
#endif
        }

        internal static void InvalidatePositiveScores(LevelCompletionResults results) {
#if BEAT_SABER_1_45_3
            var score = results.score;
            if (score.modified > 0 || score.multiplied > 0) {
                ((LevelCompletionResultsBase)results).SetField(nameof(LevelCompletionResultsBase.score),
                    new LevelScore(score.multiplied > 0 ? -score.multiplied : score.multiplied,
                        score.modified > 0 ? -score.modified : score.modified, score.maxMultiplied, score.maxModified));
            }
#else
            if (results.modifiedScore > 0) results.SetField("modifiedScore", -results.modifiedScore);
            if (results.multipliedScore > 0) results.SetField("multipliedScore", -results.multipliedScore);
#endif
        }
    }
}
