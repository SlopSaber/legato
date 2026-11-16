namespace Legato.Gameplay {
    internal static class RankAccess {
        internal static RankModel.Rank ForScore(int multiplied, int modified, int maxMultiplied, int maxModified) {
#if BEAT_SABER_1_45_3
            var score = new LevelScore(multiplied, modified, maxMultiplied, maxModified);
            return RankModel.GetRankForScore(in score);
#else
            return RankModel.GetRankForScore(System.Math.Min(multiplied, maxMultiplied), System.Math.Min(modified, maxModified), maxMultiplied, maxModified);
#endif
        }
    }
}
