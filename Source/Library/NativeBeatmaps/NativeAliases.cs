#if BEAT_SABER_1_29_0
global using BeatmapLevel = global::IPreviewBeatmapLevel;
global using BeatmapKey = global::IDifficultyBeatmap;
global using BeatmapLevelPack = global::IBeatmapLevelPack;
global using BeatmapCharacteristic = global::BeatmapCharacteristicSO;
global using BeatmapCharacteristicCollection = global::BeatmapCharacteristicCollectionSO;
global using EnvironmentsListModel = global::EnvironmentsListSO;
global using IEnvironmentInfo = global::EnvironmentInfoSO;
global using SpriteAsyncLoader = global::CachedMediaAsyncLoader;
global using CustomLevelFolderInfo = Legato.Beatmaps.Formats.LegacyLevelFolderInfo;
global using NativeLoadedSaveData = Legato.Beatmaps.Formats.LegacyLoadedSaveData;
global using NativeV4LevelInfo = Legato.Beatmaps.Formats.V4LevelInfo;
#else
global using NativeLoadedSaveData = global::CustomLevelLoader.LoadedSaveData;
global using NativeV4LevelInfo = BeatmapLevelSaveDataVersion4.BeatmapLevelSaveData;
#endif
