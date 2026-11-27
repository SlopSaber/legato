using HMUI;
using TMPro;
using UnityEngine;
#if !LEGATO_EARLY_VR_UI
using UnityEngine.Experimental.Rendering;
#endif
using UnityEngine.Networking;
using VRUIControls;

namespace Legato.Unity
{
    /// <summary>
    /// Adapts native controls and texture APIs used by BSML.
    /// </summary>
    internal static class BsmlNativeAccess
    {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || LEGATO_HMUI_LEGACY_GRAPHICS
        private static readonly System.Action<TextSlider> UpdateSliderVisuals =
            (System.Action<TextSlider>)typeof(TextSlider).GetMethod(
                "UpdateVisuals",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .CreateDelegate(typeof(System.Action<TextSlider>));
#endif

#if !LEGATO_EARLY_VR_UI
        /// <summary>
        /// Gets the engine flags for textures that will receive copied pixels.
        /// </summary>
        internal static TextureCreationFlags CopyTextureFlags =>
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || LEGATO_HMUI_LEGACY_GRAPHICS
            TextureCreationFlags.None;
#else
            TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate;
#endif
#endif

        /// <summary>
        /// Gets the native controller selected by the VR pointer.
        /// </summary>
        /// <param name="pointer">The active VR pointer.</param>
        /// <returns>The selected controller.</returns>
        internal static VRController SelectedController(VRPointer pointer) =>
#if BEAT_SABER_1_29_0 || LEGATO_HMUI_ACTIVATION_ENUMS || LEGATO_EARLY_VR_UI
            pointer.vrController;
#else
            pointer.lastSelectedVrController;
#endif

        /// <summary>
        /// Reads the native vertical navigation axis.
        /// </summary>
        /// <param name="controller">The VR controller.</param>
        /// <returns>The current vertical axis value.</returns>
        internal static float VerticalAxis(VRController controller) =>
#if BEAT_SABER_1_29_0 || LEGATO_HMUI_ACTIVATION_ENUMS || LEGATO_EARLY_VR_UI
            controller.verticalAxisValue;
#else
            controller.thumbstick.y;
#endif

        /// <summary>
        /// Updates the slider's visual state after its value changes.
        /// </summary>
        /// <param name="slider">The slider to refresh.</param>
        internal static void RefreshSlider(RangeValuesTextSlider slider)
        {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || LEGATO_HMUI_LEGACY_GRAPHICS
            UpdateSliderVisuals(slider);
#else
            slider.Refresh();
#endif
        }

        /// <summary>
        /// Checks the result of a completed Unity web request.
        /// </summary>
        /// <param name="request">The completed request.</param>
        /// <returns>Whether the request succeeded.</returns>
        internal static bool WebRequestSucceeded(UnityWebRequest request) =>
#if BEAT_SABER_1_29_0 || LEGATO_HMUI_ACTIVATION_ENUMS || LEGATO_EARLY_VR_UI
            !request.isNetworkError && !request.isHttpError;
#else
            request.result == UnityWebRequest.Result.Success;
#endif

        /// <summary>
        /// Copies enabled shader keywords from one material to another.
        /// </summary>
        /// <param name="source">The material with the desired keywords.</param>
        /// <param name="destination">The material to update.</param>
        internal static void CopyKeywords(Material source, Material destination)
        {
#if BEAT_SABER_1_29_0 || LEGATO_HMUI_LEGACY_GRAPHICS || LEGATO_EARLY_VR_UI
            destination.shaderKeywords = source.shaderKeywords;
#else
            destination.enabledKeywords = source.enabledKeywords;
#endif
        }

        /// <summary>
        /// Gets the text component that owns the submesh.
        /// </summary>
        /// <param name="mesh">The native text submesh.</param>
        /// <returns>The owning text component.</returns>
        internal static TMP_Text GetTextComponent(TMP_SubMeshUI mesh) =>
#if LEGATO_EARLY_VR_UI
            mesh.GetComponentInParent<TMP_Text>();
#elif BEAT_SABER_1_29_0 || LEGATO_HMUI_PLAIN_TMP
            mesh.m_TextComponent;
#else
            mesh.textComponent;
#endif
    }
}
