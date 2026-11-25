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
#if LEGATO_LEGACY_PACK_SORT
                false,
#endif
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
#if BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
                notAllowedCharacteristics == null ? null : Array.ConvertAll(notAllowedCharacteristics, characteristic => characteristic.AsEnum()),
#else
                notAllowedCharacteristics,
#endif
                true);
#endif
        }
    }
}
