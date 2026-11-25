#nullable enable

namespace Legato.Gameplay
{
    internal abstract class CompletionResultsCopy : LevelCompletionResults
    {
#if LEGATO_BEFORE_MULTIPLAYER
        protected CompletionResultsCopy(LevelCompletionResults lcr, int levelNotesCount) : base(
            levelNotesCount, lcr.beatmapObjectExecutionRatings, lcr.gameplayModifiers, lcr.gameplayModifiersModel,
            lcr.rawScore, lcr.modifiedScore, lcr.maxCombo, lcr.saberActivityValues, lcr.leftSaberMovementDistance,
            lcr.rightSaberMovementDistance, lcr.handActivityValues, lcr.leftHandMovementDistance,
            lcr.rightHandMovementDistance, lcr.songDuration, lcr.levelEndStateType, lcr.levelEndAction, lcr.energy, lcr.endSongTime)
        {
        }
#else
        protected CompletionResultsCopy(LevelCompletionResults lcr) : base(lcr.gameplayModifiers,
#if BEAT_SABER_1_45_3
            lcr.score,
#else
            lcr.modifiedScore, lcr.multipliedScore,
#endif
            lcr.rank, lcr.fullCombo, lcr.leftSaberMovementDistance, lcr.rightSaberMovementDistance, lcr.leftHandMovementDistance,
            lcr.rightHandMovementDistance, lcr.levelEndStateType, lcr.levelEndAction, lcr.energy, lcr.goodCutsCount,
            lcr.badCutsCount,
#if BEAT_SABER_1_45_3
            lcr.missedCutsCount,
#else
            lcr.missedCount,
#endif
            lcr.notGoodCount, lcr.okCount, lcr.maxCutScore, lcr.totalCutScore, lcr.goodCutsCountForNotesWithFullScoreScoringType, lcr.averageCenterDistanceCutScoreForNotesWithFullScoreScoringType, lcr.averageCutScoreForNotesWithFullScoreScoringType,
            lcr.maxCombo, lcr.endSongTime
#if !BEAT_SABER_1_29_0
            , lcr.invalidated
#endif
#if BEAT_SABER_1_45_3
            , lcr.startSongTime
#endif
            )
        {
        }
#endif
    }
}
