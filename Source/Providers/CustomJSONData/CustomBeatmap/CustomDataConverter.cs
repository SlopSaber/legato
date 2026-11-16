using System;
using Newtonsoft.Json;

namespace CustomJSONData.CustomBeatmap
{
#if BEAT_SABER_1_29_0
    public class CustomDataConverter : JsonConverter
#else
    public class CustomDataConverter : JsonConverter<CustomData>
#endif
    {
        public override bool CanWrite => false;

#if BEAT_SABER_1_29_0
        public override bool CanConvert(Type objectType) => typeof(CustomData).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
#else
        public override void WriteJson(JsonWriter writer, CustomData? value, JsonSerializer serializer)
#endif
        {
            throw new NotImplementedException();
        }

#if BEAT_SABER_1_29_0
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
#else
        public override CustomData ReadJson(JsonReader reader, Type objectType, CustomData? existingValue, bool hasExistingValue, JsonSerializer serializer)
#endif
        {
            return CustomData.FromJSON(reader);
        }
    }
}
