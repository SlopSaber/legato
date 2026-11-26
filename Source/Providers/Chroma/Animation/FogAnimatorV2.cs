using System;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Deserialize;
using Heck.Event;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using static Chroma.ChromaController;
using Object = UnityEngine.Object;

namespace Chroma.Animation;

[CustomEvent(ASSIGN_FOG_TRACK)]
internal class FogAnimatorV2 : ITickable, IDisposable, ICustomEvent
{
    private readonly BloomFogSO _bloomFog;
    private readonly DeserializedData _deserializedData;

    private readonly BloomFogEnvironmentParams _transitionFogParams;
#if LEGATO_CHROMA_LEGACY_LIGHTS
    private BloomFogEnvironmentParams? _previousFogParams;
    private float _previousTransition;
#endif

    private Track? _track;

    [UsedImplicitly]
    private FogAnimatorV2(
        BloomFogSO bloomFog,
        [Inject(Id = ID)] DeserializedData deserializedData)
    {
        _bloomFog = bloomFog;
        _deserializedData = deserializedData;

        _transitionFogParams = ScriptableObject.CreateInstance<BloomFogEnvironmentParams>();
#if !LEGATO_CHROMA_LEGACY_LIGHTS
        BloomFogEnvironmentParams defaultParams = bloomFog.defaultForParams;
        _transitionFogParams.attenuation = defaultParams.attenuation;
        _transitionFogParams.offset = defaultParams.offset;
        _transitionFogParams.heightFogStartY = defaultParams.heightFogStartY;
        _transitionFogParams.heightFogHeight = defaultParams.heightFogHeight;
        _transitionFogParams.autoExposureLimit = defaultParams.autoExposureLimit;
        _transitionFogParams.noteSpawnIntensity = defaultParams.noteSpawnIntensity;
#if !PRE_V1_37_1
        _transitionFogParams.legacyAutoExposure = defaultParams.legacyAutoExposure;
#endif
        bloomFog.transitionFogParams = _transitionFogParams;
#endif
    }

    public void Callback(CustomEventData customEventData)
    {
        if (_deserializedData.Resolve(customEventData, out ChromaAssignFogEventData? chromaData))
        {
            _track = chromaData.Track;
        }
    }

    public void Dispose()
    {
#if LEGATO_CHROMA_LEGACY_LIGHTS
        if (_bloomFog.fog1Params == _transitionFogParams)
        {
            _bloomFog.fog1Params = _previousFogParams;
            _bloomFog.transition = _previousTransition;
            _bloomFog.UpdateShaderParams();
        }
#else
        _bloomFog.transition = 0;
        _bloomFog.transitionFogParams = null;
#endif
        Object.Destroy(_transitionFogParams);
    }

    public void Tick()
    {
        if (_track == null)
        {
            return;
        }

#if LEGATO_CHROMA_LEGACY_LIGHTS
        if (_bloomFog.fog0Params == null)
        {
            return;
        }

        if (_bloomFog.fog1Params != _transitionFogParams)
        {
            _previousFogParams = _bloomFog.fog1Params;
            _previousTransition = _bloomFog.transition;
            _transitionFogParams.attenuation = _bloomFog.fog0Params.attenuation;
            _transitionFogParams.offset = _bloomFog.fog0Params.offset;
            _bloomFog.fog1Params = _transitionFogParams;
        }
#endif
        float? attenuation = _track.GetProperty<float>(V2_ATTENUATION);
        if (attenuation.HasValue)
        {
            _transitionFogParams.attenuation = attenuation.Value;
        }

        float? offset = _track.GetProperty<float>(V2_OFFSET);
        if (offset.HasValue)
        {
            _transitionFogParams.offset = offset.Value;
        }

#if LEGATO_CHROMA_LEGACY_LIGHTS
        _bloomFog.transition = 1;
        _bloomFog.UpdateShaderParams();
#else
        float? startY = _track.GetProperty<float>(V2_HEIGHT_FOG_STARTY);
        if (startY.HasValue)
        {
            _transitionFogParams.heightFogStartY = startY.Value;
        }

        float? height = _track.GetProperty<float>(V2_HEIGHT_FOG_HEIGHT);
        if (height.HasValue)
        {
            _transitionFogParams.heightFogHeight = height.Value;
        }

        _bloomFog._transition = 1;
#endif
    }
}
