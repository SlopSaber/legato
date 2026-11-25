using TMPro;
using UnityEngine;

namespace Legato.Rendering {
    internal static class NativeGameVisuals {
        internal static void OverrideFailedText(LevelFailedTextEffect effect, string value) {
#if BEAT_SABER_1_29_0
            var text = effect.GetComponentInChildren<TMP_Text>(true);
            if (text == null) return;
            text.richText = true;
            text.text = value;
#else
            var text = effect.GetComponentInChildren<LevelFailedText>(true);
            if (text == null) return;
            text._text.richText = true;
            text.OverrideText(value);
#endif
        }
        internal static bool IsModalShown(HMUI.ModalView modal) =>
#if LEGATO_MODAL_IS_SHOWN
            modal.isShown;
#else
            modal._isShown;
#endif
        internal static void CopyLegacyExposure(BloomFogEnvironmentParams target, BloomFogEnvironmentParams source) {
#if !BEAT_SABER_1_29_0
            target.legacyAutoExposure = source.legacyAutoExposure;
#endif
        }
        internal static void UseBasicHaptics(HapticPresetSO preset) {
#if !BEAT_SABER_1_29_0
            preset._useAdvancedHapticsOnSupportedPlatforms = false;
#endif
        }
        internal static float ShaderTime =>
#if LEGATO_STATIC_TIME_HELPER
            TimeHelper.time;
#else
            TimeHelper.GetShaderTimeValue();
#endif
    }
}
