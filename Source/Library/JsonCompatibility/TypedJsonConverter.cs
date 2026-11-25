#nullable enable
using System;
using Newtonsoft.Json;

namespace Legato.Json
{
    public abstract class TypedJsonConverter<T> :
#if BEAT_SABER_1_29_0
        JsonConverter
#else
        JsonConverter<T>
#endif
    {
#if BEAT_SABER_1_29_0
        public sealed override bool CanConvert(Type objectType) => typeof(T).IsAssignableFrom(objectType);

        public abstract T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer);

        public abstract void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer);

        public sealed override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            bool hasExistingValue = existingValue != null;
            if (hasExistingValue && !(existingValue is T))
            {
                throw new JsonSerializationException("The existing value has an incompatible type for " + typeof(T).FullName + ".");
            }

            return this.ReadJson(reader, objectType, hasExistingValue ? (T)existingValue! : default, hasExistingValue, serializer);
        }

        public sealed override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if ((value == null && default(T) is not null) || (value != null && !(value is T)))
            {
                throw new JsonSerializationException("The value has an incompatible type for " + typeof(T).FullName + ".");
            }

            this.WriteJson(writer, value == null ? default : (T)value, serializer);
        }
#endif
    }
}
