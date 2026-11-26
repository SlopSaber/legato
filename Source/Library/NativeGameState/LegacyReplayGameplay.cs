#if LEGATO_BEFORE_MULTIPLAYER
using System;
using IPA.Utilities;
using UnityEngine;

namespace Legato.Gameplay {
    internal static class LegacyReplayGameplay {
        internal static NoteCutInfo CopyCut(NoteCutInfo cut, bool scored, float before, float after) {
            var counter = scored
                ? new SaberSwingRatingCounter {
                    _beforeCutRating = Mathf.Clamp01(before),
                    _afterCutRating = Mathf.Clamp01(after)
                }
                : null;
            return new NoteCutInfo(cut.speedOK, cut.directionOK, cut.saberTypeOK, cut.wasCutTooSoon,
                cut.saberSpeed, cut.saberDir, cut.saberType, cut.timeDeviation, cut.cutDirDeviation,
                cut.cutPoint, cut.cutNormal, counter, cut.cutDistanceToCenter);
        }

        internal static void FinishCut(NoteCutInfo cut) {
            var counter = cut.swingRatingCounter;
            if (counter == null || counter.didFinish) return;
            counter._didFinish = true;
            counter.GetField<SaberSwingRatingCounter.DidFinishDelegate, SaberSwingRatingCounter>("didFinishEvent")?.Invoke(counter);
        }

        internal static void ApplyCut(ScoreController score, GameEnergyCounter energy, INoteController note, NoteCutInfo cut) {
            score.HandleNoteWasCutEvent(note, cut);
            FinishCut(cut);
            energy.HandleNoteWasCutEvent(note, cut);
            score.LateUpdate();
        }

        internal static void ApplyMiss(ScoreController score, GameEnergyCounter energy, INoteController note) {
            score.HandleNoteWasMissedEvent(note);
            energy.HandleNoteWasMissedEvent(note);
            score.LateUpdate();
        }

        internal static void ApplyWall(ScoreController score, GameEnergyCounter energy, float recordedEnergy) {
            score.LoseMultiplier(out var comboChanged, out var multiplierChanged);
            score.NotifyForChange(comboChanged, multiplierChanged);
            if (!energy.noFail && energy.energy > 0f && recordedEnergy is >= 0f and <= 1f) {
                RestoreEnergy(energy, recordedEnergy);
            }
        }

        internal static void ApplyRecordedScore(ScoreController score, int rawScore, int immediateMaximum) {
            score._prevFrameRawScore = rawScore;
            score._immediateMaxPossibleRawScore = immediateMaximum;
            var multiplier = score.gameplayModifiersScoreMultiplier;
            score.GetField<Action<int, int>, ScoreController>("scoreDidChangeEvent")?.Invoke(rawScore,
                ScoreModel.GetModifiedScoreForGameplayModifiersScoreMultiplier(rawScore, multiplier));
            score.GetField<Action<int, int>, ScoreController>("immediateMaxPossibleScoreDidChangeEvent")?.Invoke(immediateMaximum,
                ScoreModel.GetModifiedScoreForGameplayModifiersScoreMultiplier(immediateMaximum, multiplier));
        }

        internal static void ApplyRecordedCombo(ScoreController score, int combo, int maximumCombo) {
            score._combo = combo;
            score._maxCombo = maximumCombo;
            score.NotifyForChange(comboChanged: true, multiplierChanged: false);
        }

        internal static void ApplyRecordedMultiplier(ScoreController score, int multiplier, float progress) {
            score._multiplier = multiplier;
            score._multiplierIncreaseMaxProgress = multiplier * 2;
            score._multiplierIncreaseProgress = Mathf.RoundToInt(progress * score._multiplierIncreaseMaxProgress);
            score.NotifyForChange(comboChanged: false, multiplierChanged: true);
        }

        internal static void Reset(ScoreController score, GameEnergyCounter energy) {
            score._baseRawScore = 0;
            score._multiplier = 1;
            score._multiplierIncreaseProgress = 0;
            score._multiplierIncreaseMaxProgress = 2;
            score._combo = 0;
            score._maxCombo = 0;
            score._cutOrMissedNotes = 0;
            score._cutScoreBuffers.Clear();
            score.NotifyForChange(comboChanged: true, multiplierChanged: true);
            score.LateUpdate();
            RestoreEnergy(energy, energy.energyType == GameplayModifiers.EnergyType.Battery ? 1f : 0.5f);
        }

        internal static void RestoreEnergy(GameEnergyCounter energy, float value, bool notifyFailure = true) {
            var reachedZero = energy.energy > 0f && value == 0f;
            energy.energy = value;
            if (reachedZero && notifyFailure) {
                energy.GetField<Action, GameEnergyCounter>("gameEnergyDidReach0Event")?.Invoke();
            }
            energy.GetField<Action<float>, GameEnergyCounter>("gameEnergyDidChangeEvent")?.Invoke(value);
        }
    }
}
#endif
