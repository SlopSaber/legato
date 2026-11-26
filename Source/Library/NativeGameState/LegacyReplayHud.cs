#if LEGATO_BEFORE_MULTIPLAYER
using System;

namespace Legato.Gameplay {
    internal static class LegacyReplayHud {
        internal static void ShowCombo(ScoreController score, ComboUIController? panel, int combo, int maxCombo, bool broken) {
            score._combo = combo;
            score._maxCombo = maxCombo;
            if (panel == null || !panel.isActiveAndEnabled) return;

            panel.HandleComboDidChange(combo);
            panel._comboLost = false;
            if (panel._animator != null) panel._animator.Rebind();
            if (broken) panel.HandleComboBreakingEventHappened();
        }

        internal static void RefreshCombo(ScoreController score, ComboUIController? panel, bool broken) =>
            ShowCombo(score, panel, score._combo, score._maxCombo, broken);

        internal static void RefreshEnergy(GameEnergyUIPanel? panel, float energy) {
            if (panel != null && panel.isActiveAndEnabled) panel.HandleGameEnergyDidChange(energy);
        }

        internal static void StopObstacleSparks(ObstacleSaberSparkleEffectManager? manager) {
            if (manager == null || manager._effects == null) return;
            foreach (var effect in manager._effects) {
                if (effect != null) effect.StopEmission();
            }
        }

        internal static void StopBurnMarks(SaberBurnMarkSparkles sparkles) {
            if (sparkles._burnMarksPS == null) return;
            Array.Clear(sparkles._prevBurnMarkPosValid, 0, sparkles._prevBurnMarkPosValid.Length);
            foreach (var system in sparkles._burnMarksPS) {
                if (system == null) continue;
                var emission = system.emission;
                emission.enabled = false;
            }
        }
    }
}
#endif
