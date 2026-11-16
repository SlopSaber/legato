#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

#if LEGATO_BSML_ALIASED
using Bsml::BeatSaberMarkupLanguage;
#else
using BeatSaberMarkupLanguage;
#endif
using System.Reflection;

namespace Legato.Resources {
    internal static class EmbeddedResources {
        internal static byte[] Read(Assembly assembly, string resource) {
#pragma warning disable CS0618
            return Utilities.GetResource(assembly, resource);
#pragma warning restore CS0618
        }
    }
}
