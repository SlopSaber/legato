#if HITBLOQ_BS_1_29_1 || LEGATO_BEFORE_MULTIPLAYER
global using HitbloqNativePack = global::IBeatmapLevelPack;
public readonly struct BeatmapKey
{
	public BeatmapKey(string levelId, BeatmapDifficulty difficulty, BeatmapCharacteristicSO beatmapCharacteristic)
	{
		this.levelId = levelId;
		this.difficulty = difficulty;
		this.beatmapCharacteristic = beatmapCharacteristic;
	}

	public readonly string levelId;
	public readonly BeatmapDifficulty difficulty;
	public readonly BeatmapCharacteristicSO beatmapCharacteristic;

	public static BeatmapKey FromDifficultyBeatmap(IDifficultyBeatmap difficultyBeatmap)
	{
		return new BeatmapKey(difficultyBeatmap.level.levelID, difficultyBeatmap.difficulty, difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic);
	}
}
#else
global using HitbloqNativePack = global::BeatmapLevelPack;
#endif
