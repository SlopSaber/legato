#if LEGATO_BEFORE_MULTIPLAYER
using System;
using UnityEngine;

namespace Legato.Gameplay {
    internal sealed class LegacyDanceFog : IDisposable {
        private readonly BloomFogSO _fog;
        private readonly BloomFogEnvironmentParams _originalStart;
        private readonly BloomFogEnvironmentParams? _originalEnd;
        private readonly float _originalTransition;
        private readonly BloomFogEnvironmentParams _baseFog;
        private readonly BloomFogEnvironmentParams _dreadFog;

        internal LegacyDanceFog(BloomFogSO fog, float thickening, float minimumAttenuation) {
            _fog = fog;
            _originalStart = fog.fog0Params;
            _originalEnd = fog.fog1Params;
            _originalTransition = fog.transition;
            var end = _originalEnd == null ? _originalStart : _originalEnd;
            _baseFog = ScriptableObject.CreateInstance<BloomFogEnvironmentParams>();
            _baseFog.attenuation = Mathf.Lerp(_originalStart.attenuation, end.attenuation, _originalTransition);
            _baseFog.offset = Mathf.Lerp(_originalStart.offset, end.offset, _originalTransition);
            _dreadFog = ScriptableObject.CreateInstance<BloomFogEnvironmentParams>();
            _dreadFog.attenuation = Mathf.Max(_baseFog.attenuation * thickening, minimumAttenuation);
            _dreadFog.offset = _baseFog.offset * 0.3f;
            _fog.fog0Params = _baseFog;
            _fog.fog1Params = _dreadFog;
            if (_originalTransition == 0f) _fog.UpdateShaderParams();
            else _fog.transition = 0f;
        }

        internal void SetAmount(float amount) => _fog.transition = amount;

        public void Dispose() {
            if (_fog != null) {
                _fog.fog0Params = _originalStart;
                _fog.fog1Params = _originalEnd;
                if (_fog.transition == _originalTransition) _fog.UpdateShaderParams();
                else _fog.transition = _originalTransition;
            }
            UnityEngine.Object.Destroy(_baseFog);
            UnityEngine.Object.Destroy(_dreadFog);
        }
    }
}
#endif
