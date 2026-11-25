#nullable enable
using System.IO;
using Newtonsoft.Json;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using Newtonsoft.Json.Serialization;
#endif

namespace Legato.Json
{
    internal static class NativeJsonSettings
    {
        internal static readonly JsonSerializerSettings readableWithDefault =
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new DefaultContractResolver { IgnoreSerializableAttribute = false }
            };
#else
            BGLib.JsonExtension.JsonSettings.readableWithDefault;
#endif

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        internal static readonly JsonSerializerSettings compactNoDefault = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new DefaultContractResolver { IgnoreSerializableAttribute = false }
        };
#endif
    }

    internal static class NativeJsonFileHandler
    {
        internal static T ReadFromFile<T>(string path)
        {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var text = new StreamReader(stream);
            using var json = new JsonTextReader(text);
            return JsonSerializer.Create(NativeJsonSettings.readableWithDefault).Deserialize<T>(json)!;
#else
            return BGLib.JsonExtension.JsonFileHandler.ReadFromFile<T>(path);
#endif
        }

        internal static void WriteCompactWithoutDefault<T>(T value, string path)
        {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var text = new StreamWriter(stream);
            using var json = new JsonTextWriter(text);
            JsonSerializer.Create(NativeJsonSettings.compactNoDefault).Serialize(json, value);
#else
            BGLib.JsonExtension.JsonFileHandler.WriteCompactWithoutDefault(value, path);
#endif
        }
    }
}
