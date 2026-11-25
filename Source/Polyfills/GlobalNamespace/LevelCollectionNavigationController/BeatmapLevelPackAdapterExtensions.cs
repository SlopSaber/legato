#nullable enable

using System;
using UnityEngine;

namespace Legato {
    internal static class LevelCollectionBeatmapLevelPackExtensions {
        internal static void SetPackData(
            this LevelCollectionNavigationController controller,
            BeatmapLevelPackAdapter pack,
            bool showPackHeader,
            bool showPracticeButton,
            string actionButtonText,
            GameObject noDataInfoPrefab,
            BeatmapDifficultyMask allowedBeatmapDifficultyMask,
            BeatmapCharacteristicSO[] notAllowedCharacteristics) {
#if LEGATO_DIFFICULTY_BEATMAPS
            controller.SetData(
                pack.source,
                showPackHeader,
                showPracticeButton,
                actionButtonText,
                noDataInfoPrefab,
                allowedBeatmapDifficultyMask,
                notAllowedCharacteristics);
#else
            controller.SetData(
                pack.source,
                showPackHeader,
                showPracticeButton,
                actionButtonText,
                true,
                noDataInfoPrefab,
                allowedBeatmapDifficultyMask,
                notAllowedCharacteristics,
                true);
#endif
        }
    }
}
