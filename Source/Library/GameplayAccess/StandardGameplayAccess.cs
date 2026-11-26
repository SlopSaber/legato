using IPA.Utilities;
using UnityEngine;

namespace Legato.Gameplay {
    internal static class StandardGameplayAccess {
        internal static bool TryResolvePausedGameplay(PauseMenuManager owner, out StandardLevelGameplayManager manager) {
            manager = null;
            if (!owner || !owner.gameObject.activeInHierarchy || !owner.gameObject.scene.isLoaded) return false;
            foreach (var candidate in UnityEngine.Resources.FindObjectsOfTypeAll<StandardLevelGameplayManager>()) {
                if (!candidate || !candidate.isActiveAndEnabled || !candidate.gameObject.scene.isLoaded ||
#if LEGATO_BEFORE_MULTIPLAYER
                    candidate._gameState != StandardLevelGameplayManager.GameState.Paused) continue;
#else
                    candidate._initData == null ||
                    candidate._gameState == StandardLevelGameplayManager.GameState.Finished ||
                    candidate._gameState == StandardLevelGameplayManager.GameState.Failed) continue;
#endif
                var controller = candidate._pauseController;
                if (!controller || !controller.isActiveAndEnabled || !ReferenceEquals(controller._pauseMenuManager, owner)) continue;
                if (manager != null) { manager = null; return false; }
                manager = candidate;
            }
            return manager != null;
        }

        internal static void Fail(StandardLevelGameplayManager manager) {
#if LEGATO_BEFORE_MULTIPLAYER
            if (manager._gameState != StandardLevelGameplayManager.GameState.Paused) return;
#elif LEGATO_FAIL_ON_ZERO_ENERGY
            manager._initData.SetField(nameof(manager._initData.failOn0Energy), true);
#else
            manager._initData.SetField(nameof(manager._initData.continueGameplayWith0Energy), false);
#endif
            manager.HandleGameEnergyDidReach0();
        }
    }
}
