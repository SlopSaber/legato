#if LEGATO_BSML_ALIASED
extern alias Bsml;
#endif

#nullable enable

using System.Threading.Tasks;
using UnityEngine.UI;

namespace Legato {
    internal static class ImageExtensions {
        public static Task SetImageAsync(this Image image, string location) {
#if LEGATO_BSML_ALIASED
            return Bsml::BeatSaberMarkupLanguage.BeatSaberUI.SetImageAsync(image, location);
#else
            return BeatSaberMarkupLanguage.BeatSaberUI.SetImageAsync(image, location);
#endif
        }
    }
}
