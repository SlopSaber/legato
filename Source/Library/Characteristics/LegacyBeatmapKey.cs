#if BEAT_SABER_1_29_0
namespace Legato.Compatibility
{
    internal readonly struct LegacyBeatmapKey
    {
        internal LegacyBeatmapKey(string levelId, BeatmapCharacteristicSO characteristic, BeatmapDifficulty difficulty)
        {
            this.levelId = levelId;
            beatmapCharacteristic = characteristic;
            this.difficulty = difficulty;
        }

        public string levelId { get; }
        public BeatmapCharacteristicSO beatmapCharacteristic { get; }
        public BeatmapDifficulty difficulty { get; }
    }
}
#endif
