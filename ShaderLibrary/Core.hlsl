#ifndef LEGATO_SHADER_CORE_INCLUDED
#define LEGATO_SHADER_CORE_INCLUDED
#include "Pipeline.hlsl"

#if LEGATO_URP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#else
#include "UnityCG.cginc"

// The built-in backend currently targets the PC D3D11 shader compiler.
#if !defined(SHADER_API_D3D11)
#error Legato built-in shader compatibility requires Direct3D 11.
#endif
#define TEXTURE2D(name) Texture2D name
#define SAMPLER(name) SamplerState name
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord) textureName.Sample(samplerName, coord)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord, lod) textureName.SampleLevel(samplerName, coord, lod)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define TEXTURE2D_X(name) Texture2DArray name
#define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord) textureName.Sample(samplerName, float3(coord, unity_StereoEyeIndex))
#define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord, lod) textureName.SampleLevel(samplerName, float3(coord, unity_StereoEyeIndex), lod)
#else
#define TEXTURE2D_X(name) TEXTURE2D(name)
#define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord) SAMPLE_TEXTURE2D(textureName, samplerName, coord)
#define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord, lod) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord, lod)
#endif
SamplerState sampler_PointClamp;
SamplerState sampler_LinearClamp;
// Built-in camera targets occupy their full allocation; they have no RTHandle viewport padding.
#define _RTHandleScale float4(1, 1, 1, 1)

float3 TransformObjectToWorld(float3 positionOS) { return mul(unity_ObjectToWorld, float4(positionOS, 1)).xyz; }
float4 TransformObjectToHClip(float3 positionOS) { return UnityObjectToClipPos(positionOS); }
float4 TransformWorldToHClip(float3 positionWS) { return mul(UNITY_MATRIX_VP, float4(positionWS, 1)); }
float3 TransformWorldToView(float3 positionWS) { return mul(UNITY_MATRIX_V, float4(positionWS, 1)).xyz; }
float3 TransformObjectToWorldNormal(float3 normalOS) { return UnityObjectToWorldNormal(normalOS); }
float3 GetCameraPositionWS() { return _WorldSpaceCameraPos; }
float4 GetScaledScreenParams() { return _ScreenParams; }
float2 GetNormalizedScreenSpaceUV(float2 positionCS) { return positionCS / _ScreenParams.xy; }
float2 GetNormalizedScreenSpaceUV(float4 positionCS) { return GetNormalizedScreenSpaceUV(positionCS.xy); }
float LinearEyeDepth(float depth, float4 parameters) { return rcp(parameters.z * depth + parameters.w); }
float LinearDepthToEyeDepth(float depth)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif
    return lerp(_ProjectionParams.y, _ProjectionParams.z, depth);
}
float4 GetFullScreenTriangleVertexPosition(uint vertexID)
{
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
    return float4(uv * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
}
float2 GetFullScreenTriangleTexCoord(uint vertexID)
{
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1 - uv.y;
#endif
    return uv;
}
// URP's ComputeScreenPos does not apply the packed-eye transform. Callers apply it when sampling.
#define ComputeScreenPos ComputeNonStereoScreenPos
#endif
#endif
