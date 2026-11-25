using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Legato.Assets
{
    internal static class EmbeddedBundleProfile
    {
        internal static string Select(string legacy, string current)
        {
#if LEGATO_BUILT_IN_RENDERING
            return legacy;
#else
            return current;
#endif
        }

        internal static AssetBundle Load(Assembly assembly, string legacyResource, string currentResource)
        {
            string name = Select(legacyResource, currentResource);
            using (var stream = assembly.GetManifestResourceStream(name))
            using (var buffer = new MemoryStream())
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("The embedded asset bundle was not found: " + name);
                }

                stream.CopyTo(buffer);
                var bundle = AssetBundle.LoadFromMemory(buffer.ToArray());
                if (bundle == null)
                {
                    throw new InvalidOperationException("The embedded asset bundle could not be loaded: " + name);
                }

                return bundle;
            }
        }
    }
}
