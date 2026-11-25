using CustomJSONData.CustomBeatmap;

namespace Chroma.Lighting;

internal static class LightEventAccess
{
    internal static BasicBeatmapEventType GetEventType(this BasicBeatmapEventData item) =>
#if LEGATO_CHROMA_LEGACY_LIGHTS
        item.type;
#else
        item.basicBeatmapEventType;
#endif

    internal static float GetFloatValue(this BasicBeatmapEventData item) =>
#if LEGATO_CHROMA_LEGACY_LIGHTS
        item is CustomBasicBeatmapEventData custom ? custom.floatValue : 1f;
#else
        item.floatValue;
#endif
}
