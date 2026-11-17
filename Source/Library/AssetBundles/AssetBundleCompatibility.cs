using System;
using System.IO;
#if BEAT_SABER_1_29_0
using System.Text;
using UnityEngine;
#endif

namespace Legato.Assets
{
    internal static class AssetBundleCompatibility
    {
        internal static bool IsSupported(string path, Action<string>? warning)
        {
#if BEAT_SABER_1_29_0
            try
            {
                using (var stream = File.OpenRead(path)) return IsSupported(stream, warning);
            }
            catch (IOException) { }
#endif
            return true;
        }

        internal static bool IsSupported(byte[] data, Action<string>? warning)
        {
#if BEAT_SABER_1_29_0
            if (data != null)
            {
                using (var stream = new MemoryStream(data, false)) return IsSupported(stream, warning);
            }
#endif
            return true;
        }

        internal static bool IsSupported(Stream stream, Action<string>? warning)
        {
#if BEAT_SABER_1_29_0
            if (stream == null || !stream.CanSeek || !stream.CanRead) return true;
            long position = stream.Position;
            try
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    string signature = ReadHeaderString(reader);
                    if (signature != "UnityFS" && signature != "UnityRaw" && signature != "UnityWeb") return true;
                    reader.ReadInt32();
                    ReadHeaderString(reader);
                    string editor = ReadHeaderString(reader);
                    int dot = editor.IndexOf('.');
                    int runtimeDot = Application.unityVersion.IndexOf('.');
                    if (dot > 0 && runtimeDot > 0 && int.TryParse(editor.Substring(0, dot), out int builtMajor) &&
                        int.TryParse(Application.unityVersion.Substring(0, runtimeDot), out int runtimeMajor) && builtMajor > runtimeMajor)
                    {
                        warning?.Invoke($"Asset bundle was built with Unity {editor}; this player uses Unity {Application.unityVersion}. Rebuild the asset for this game profile.");
                        return false;
                    }
                }
            }
            catch (EndOfStreamException) { }
            finally { stream.Position = position; }
#endif
            return true;
        }

#if BEAT_SABER_1_29_0
        private static string ReadHeaderString(BinaryReader reader)
        {
            var text = new StringBuilder();
            for (int index = 0; index < 64; index++)
            {
                byte value = reader.ReadByte();
                if (value == 0) return text.ToString();
                text.Append((char)value);
            }
            return string.Empty;
        }
#endif
    }
}
