using Zenject;

namespace Hitbloq.Other {
    internal class BeatmapReporter : IInitializable {
        private readonly BeatmapListener _beatmapListener;
        private readonly BeatmapKey _beatmapKey;

        public BeatmapReporter(
#if HITBLOQ_BS_1_29_1 || LEGATO_BEFORE_MULTIPLAYER
            IDifficultyBeatmap difficultyBeatmap,
#else
            BeatmapKey beatmapKey,
#endif
            BeatmapListener beatmapListener) {
#if HITBLOQ_BS_1_29_1 || LEGATO_BEFORE_MULTIPLAYER
            _beatmapKey = BeatmapKey.FromDifficultyBeatmap(difficultyBeatmap);
#else
            _beatmapKey = beatmapKey;
#endif
            _beatmapListener = beatmapListener;
        }

        public void Initialize() {
            _beatmapListener.LastPlayedBeatmapKey = _beatmapKey;
        }
    }
}
