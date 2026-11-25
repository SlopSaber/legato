#nullable enable

using System.Linq;
using UnityEngine;

namespace Legato {
    internal static class LevelCollectionViewBeatmapLevelPackExtensions {
        internal static void SetData(
            this LevelCollectionViewController controller,
            BeatmapLevelPackAdapter pack,
            string headerText,
            Sprite headerSprite,
            GameObject noDataInfoPrefab) {
#if LEGATO_DIFFICULTY_BEATMAPS
            controller.SetData(pack.source.beatmapLevelCollection, headerText, headerSprite, false,
#if LEGATO_LEGACY_PACK_SORT
                false,
#endif
                noDataInfoPrefab);
#else
            controller.SetData(
                pack.beatmapLevels.Select(level => level.source).ToArray(),
                headerText,
                headerSprite,
                false,
                false,
                noDataInfoPrefab);
#endif
        }
    }
}
