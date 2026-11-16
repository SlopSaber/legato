using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;
using VRUIControls;

namespace Legato.Unity
{
    internal static class BsmlNativeAccess
    {
#if BEAT_SABER_1_29_0
        private static readonly System.Action<TextSlider> UpdateSliderVisuals =
            (System.Action<TextSlider>)typeof(TextSlider).GetMethod("UpdateVisuals",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .CreateDelegate(typeof(System.Action<TextSlider>));
#endif

        internal static TextureCreationFlags CopyTextureFlags =>
#if BEAT_SABER_1_29_0
            TextureCreationFlags.None;
#else
            TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate;
#endif

        internal static VRController SelectedController(VRPointer pointer) =>
#if BEAT_SABER_1_29_0
            pointer.vrController;
#else
            pointer.lastSelectedVrController;
#endif

        internal static float VerticalAxis(VRController controller) =>
#if BEAT_SABER_1_29_0
            controller.verticalAxisValue;
#else
            controller.thumbstick.y;
#endif

        internal static void RefreshSlider(RangeValuesTextSlider slider)
        {
#if BEAT_SABER_1_29_0
            UpdateSliderVisuals(slider);
#else
            slider.Refresh();
#endif
        }

        internal static bool WebRequestSucceeded(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0
            !request.isNetworkError && !request.isHttpError;
#else
            request.result == UnityWebRequest.Result.Success;
#endif

        internal static void CopyKeywords(Material source, Material destination)
        {
#if BEAT_SABER_1_29_0
            destination.shaderKeywords = source.shaderKeywords;
#else
            destination.enabledKeywords = source.enabledKeywords;
#endif
        }

        internal static TextMeshProUGUI GetTextComponent(TMP_SubMeshUI mesh) =>
#if BEAT_SABER_1_29_0
            mesh.m_TextComponent;
#else
            mesh.textComponent;
#endif
    }
}
