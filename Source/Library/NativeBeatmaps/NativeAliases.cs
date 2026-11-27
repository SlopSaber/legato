#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
global using BeatmapLevel = global::IStandardLevel;
global using BeatmapKey = global::IStandardLevelDifficultyBeatmap;
global using BeatmapDifficulty = global::LevelDifficulty;
#elif BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
global using BeatmapLevel = global::IPreviewBeatmapLevel;
global using BeatmapKey = global::IDifficultyBeatmap;
global using NativeSongPack = global::IBeatmapLevelPack;
global using NativeSongCollection = global::IAnnotatedBeatmapLevelCollection;
global using BeatmapCharacteristic = global::BeatmapCharacteristicSO;
global using BeatmapCharacteristicCollection = global::BeatmapCharacteristicCollectionSO;
global using EnvironmentsListModel = global::EnvironmentsListSO;
global using IEnvironmentInfo = global::EnvironmentInfoSO;
global using SpriteAsyncLoader = global::CachedMediaAsyncLoader;

#else
global using NativeSongPack = global::BeatmapLevelPack;
global using NativeSongCollection = global::BeatmapLevelPack;
#if LEGATO_SCRIPTABLE_CHARACTERISTICS
global using BeatmapCharacteristic = global::BeatmapCharacteristicSO;
#endif
#endif
