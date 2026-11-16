using TMPro;

namespace Legato.Unity
{
    internal static class TextWrapping
    {
        internal static bool Get(TMP_Text text)
        {
#if BEAT_SABER_1_29_0
            return text.enableWordWrapping;
#else
            return text.textWrappingMode == TextWrappingModes.Normal;
#endif
        }

        internal static void Set(TMP_Text text, bool enabled)
        {
#if BEAT_SABER_1_29_0
            text.enableWordWrapping = enabled;
#else
            text.textWrappingMode = enabled ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
#endif
        }
    }
}
