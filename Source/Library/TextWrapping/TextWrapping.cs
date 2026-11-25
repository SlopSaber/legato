using TMPro;

namespace Legato.Unity
{
    internal static class TextWrapping
    {
        internal static bool Get(TMP_Text text)
        {
#if LEGATO_HISTORICAL_GAME || BEAT_SABER_1_34_2 || LEGATO_LEGACY_TEXTMESH_PRO
            return text.enableWordWrapping;
#else
            return text.textWrappingMode == TextWrappingModes.Normal;
#endif
        }

        internal static void Set(TMP_Text text, bool enabled)
        {
#if LEGATO_HISTORICAL_GAME || BEAT_SABER_1_34_2 || LEGATO_LEGACY_TEXTMESH_PRO
            text.enableWordWrapping = enabled;
#else
            text.textWrappingMode = enabled ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
#endif
        }
    }
}
