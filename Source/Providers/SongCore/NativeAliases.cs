#if BEAT_SABER_1_29_0
global using CustomLevelFolderInfo = Legato.Beatmaps.Formats.LegacyLevelFolderInfo;
global using NativeLoadedSaveData = Legato.Beatmaps.Formats.LegacyLoadedSaveData;
global using NativeV4LevelInfo = Legato.Beatmaps.Formats.V4LevelInfo;
global using NativeEntitlements = global::AdditionalContentModel;
#else
global using NativeEntitlements = global::IEntitlementModel;
global using NativeLoadedSaveData = global::CustomLevelLoader.LoadedSaveData;
global using NativeV4LevelInfo = BeatmapLevelSaveDataVersion4.BeatmapLevelSaveData;
#endif
