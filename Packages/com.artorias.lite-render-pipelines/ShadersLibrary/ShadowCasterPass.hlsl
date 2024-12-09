#pragma once

#include "Shadows.hlsl"
#include "SurfaceData.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct FAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS     : NORMAL;
    float2 texCoord     : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FVaryings
{
    float4 positionCS   : SV_POSITION;
    
    #if defined(_ALPHATEST_ON)
    float2 texCoord       : TEXCOORD0;
    #endif
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 GetShadowPositionHClip(FAttributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 lightDirectionWS = _LightDirection;
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    return positionCS;
}

FVaryings ShadowPassVertex(FAttributes input)
{
    FVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    #if defined(_ALPHATEST_ON)
    output.texCoord = TRANSFORM_TEX(input.texCoord, _BaseMap);
    #endif

    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

half4 ShadowPassFragment(FVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    #if defined(_ALPHATEST_ON)
    Alpha(SampleAlbedoAlpha(input.texCoord, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    return 0;
}
