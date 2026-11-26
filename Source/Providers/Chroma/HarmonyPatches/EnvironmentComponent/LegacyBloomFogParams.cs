#if LEGATO_CHROMA_LEGACY_LIGHTS
using UnityEngine;

namespace Chroma.HarmonyPatches.EnvironmentComponent;

internal sealed class LegacyBloomFogParams : MonoBehaviour
{
    private BloomFogSO _bloomFog = null!;
    private BloomFogEnvironmentParams? _original;
    private BloomFogEnvironmentParams? _parameters;

    internal void Initialize(BloomFogEnvironment environment)
    {
        _bloomFog = environment._bloomFog;
        _original = environment._fog0Params;
        if (_original != null)
        {
            _parameters = Instantiate(_original);
            environment._fog0Params = _parameters;
        }
    }

    internal void SetAttenuation(float value)
    {
        if (_parameters != null)
        {
            _parameters.attenuation = value;
            Refresh();
        }
    }

    internal void SetOffset(float value)
    {
        if (_parameters != null)
        {
            _parameters.offset = value;
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_bloomFog.fog0Params == _parameters)
        {
            _bloomFog.UpdateShaderParams();
        }
    }

    private void OnDestroy()
    {
        if (_parameters == null)
        {
            return;
        }

        if (_bloomFog != null && _bloomFog.fog0Params == _parameters)
        {
            _bloomFog.fog0Params = _original;
            _bloomFog.UpdateShaderParams();
        }

        Destroy(_parameters);
    }
}
#endif
