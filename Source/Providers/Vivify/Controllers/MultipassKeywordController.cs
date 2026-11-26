#if !V1_29_1
using UnityEngine;
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using UnityEngine.XR;
#else
using UnityEngine.XR.OpenXR;
#endif

namespace Vivify.Controllers;

[RequireComponent(typeof(Camera))]
internal class MultipassKeywordController : MonoBehaviour
{
    private const string MULTIPASS_KEYWORD = "MULTIPASS_ENABLED";
    private const string MULTIPASS_EYE_KEY = "_StereoActiveEye";

    private static readonly int _multipassEye = Shader.PropertyToID(MULTIPASS_EYE_KEY);

    private Camera _camera = null!;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnPreRender()
    {
        bool enable = _camera.stereoEnabled &&
                      _camera.stereoTargetEye != StereoTargetEyeMask.None &&
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
                      XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass;
#else
                      OpenXRSettings.Instance.renderMode == OpenXRSettings.RenderMode.MultiPass;
#endif
        if (enable)
        {
            Shader.EnableKeyword(MULTIPASS_KEYWORD);
        }
        else
        {
            Shader.DisableKeyword(MULTIPASS_KEYWORD);
        }

        Shader.SetGlobalInt(_multipassEye, (int)_camera.stereoActiveEye);
    }
}
#endif
