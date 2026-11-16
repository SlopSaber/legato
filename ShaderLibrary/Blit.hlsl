#ifndef LEGATO_SHADER_BLIT_INCLUDED
#define LEGATO_SHADER_BLIT_INCLUDED
#include "Core.hlsl"
#if LEGATO_URP
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#else
TEXTURE2D_X(_BlitTexture);
float4 _BlitTexture_TexelSize;
float4 _BlitScaleBias;
struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};
Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}
#endif
#endif
