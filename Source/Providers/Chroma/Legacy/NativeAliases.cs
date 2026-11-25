#if LEGATO_CHROMA_LEGACY_LIGHTS
global using BasicBeatmapEventData = global::BeatmapEventData;
global using BasicBeatmapEventType = global::BeatmapEventType;
global using BeatmapCallbacksController = global::BeatmapObjectCallbackController;
global using BeatmapEventDataLightsExtensions = Chroma.Lighting.LegacyLightEventValues;
global using ColorType = global::NoteType;
global using EnvironmentColorType = Chroma.Lighting.LegacyEnvironmentColorType;
global using EnvironmentSceneSetupData = global::GameplayCoreSceneSetupData;
global using ILightWithId = global::LightWithId;
global using IReadonlyBeatmapData = global::BeatmapData;
global using NoteControllerBase = global::NoteController;
global using ObstacleControllerBase = global::ObstacleController;
global using SongTimeTweeningManager = Chroma.Lighting.LegacySongTimeTweening;
#endif
