#nullable enable
using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Legato.Json
{
    public class CamelCaseContractResolver : CamelCasePropertyNamesContractResolver
    {
        public CamelCaseContractResolver()
        {
#if !BEAT_SABER_1_29_0
            NamingStrategy = new CamelCaseNamingStrategy();
#endif
        }

#if BEAT_SABER_1_29_0
        protected override string ResolveDictionaryKey(string dictionaryKey) => dictionaryKey;

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var specifiedName = member.GetCustomAttribute<JsonPropertyAttribute>(true)?.PropertyName;
            if (specifiedName != null)
            {
                property.PropertyName = specifiedName;
            }

            return property;
        }
#endif
    }
}
