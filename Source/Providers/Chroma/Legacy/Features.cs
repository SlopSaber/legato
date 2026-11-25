#if LEGATO_CHROMA_LEGACY_LIGHTS
using System;
using System.Linq;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chroma.EnvironmentEnhancement.Saved;

[JsonConverter(typeof(FeaturesDataConverter))]
internal readonly struct Features
{
    private Features(bool useChromaEvents, bool forceStaticLights, BeatmapEventData[]? basicEvents)
    {
        UseChromaEvents = useChromaEvents;
        ForceStaticLights = forceStaticLights;
        BasicEvents = basicEvents;
    }

    internal bool UseChromaEvents { get; }

    internal bool ForceStaticLights { get; }

    internal BeatmapEventData[]? BasicEvents { get; }

    private sealed class FeaturesDataConverter : Legato.Json.TypedJsonConverter<Features>
    {
        public override Features ReadJson(JsonReader reader, Type objectType, Features existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject definition = JObject.Load(reader);
            bool useChromaEvents = definition.Value<bool?>("useChromaEvents") ?? false;
            bool forceStaticLights = string.Equals(definition.Value<string>("forceEffectsFilter"), "NoEffects", StringComparison.Ordinal);
            BeatmapEventData[]? events = (definition["basicBeatmapEvents"] as JArray)?.OfType<JObject>().Select(ReadEvent).ToArray();
            return new Features(useChromaEvents, forceStaticLights, events);
        }

        public override void WriteJson(JsonWriter writer, Features value, JsonSerializer serializer) => throw new NotImplementedException();

        private static BeatmapEventData ReadEvent(JObject entry)
        {
            BeatmapEventType type = (BeatmapEventType)(entry.Value<int?>("et") ?? 0);
            int value = entry.Value<int?>("i") ?? 0;
            float intensity = entry.Value<float?>("f") ?? 1f;
            CustomData data = entry["customData"] is JObject custom ? CustomData.FromJSON(custom.ToString(Formatting.None)) : new CustomData();
            return new CustomBasicBeatmapEventData(new BeatmapEventData(0, type, value), intensity, data, VersionExtensions.version3);
        }
    }
}
#endif
