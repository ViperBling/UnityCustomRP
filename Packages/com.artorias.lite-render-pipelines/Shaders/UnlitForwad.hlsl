#pragma once

#include "Packages/com.artorias.lite-render-pipelines/ShadersLibrary/SurfaceData.hlsl"
#include "Packages/com.artorias.lite-render-pipelines/ShadersLibrary/Shadows.hlsl"
#include "UnlitInput.hlsl"

struct FAttributes
{
    float4 positionOS : POSITION;
    float2 texCoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FVaryings
{
    float4 positionCS : SV_POSITION;
    float2 texCoord : TEXCOORD0;
    float fogCoord : TEXCOORD1;

    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS              : TEXCOORD2;
    #endif
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord             : TEXCOORD3;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

FVaryings UnlitPassVertex(FAttributes input)
{
    FVaryings output = (FVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    FVertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    output.positionCS = vertexInput.positionCS;
    output.texCoord = TRANSFORM_TEX(input.texCoord, _BaseMap);
    
#if defined(_FOG_FRAGMENT)
    output.fogCoord = vertexInput.positionVS.z;
#else
    output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
#endif

    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
        output.positionWS = vertexInput.positionWS;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    return output;
}

half4 UnlitPassFragment(FVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    half shadow = 1.0;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord = input.shadowCoord;
    half shadowFade = half(1.0);
    shadow = MainLightShadow(shadowCoord, input.positionWS);
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    half shadowFade = GetMainLightShadowFade(input.positionWS);
    shadow = MainLightShadow(shadowCoord, input.positionWS);
#else
    float4 shadowCoord = float4(0, 0, 0, 0);
    half shadowFade = half(1.0);
#endif

    FSurfaceDataUnlit unlitSurfaceData;
    
    half4 albedoAlpha = SampleAlbedoAlpha(input.texCoord, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    unlitSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);;
    unlitSurfaceData.albedo = AlphaModulate(albedoAlpha.rgb * _BaseColor.rgb, unlitSurfaceData.alpha);
    unlitSurfaceData.emission = SampleEmission(input.texCoord, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

#if defined(_FOG_FRAGMENT)
    #if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
        float viewZ = -input.fogCoord;
        float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
        half fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
    #else
        half fogFactor = 0;
    #endif
#else
    half fogFactor = input.fogCoord;
#endif

    half4 color = half4(unlitSurfaceData.albedo + unlitSurfaceData.emission, unlitSurfaceData.alpha) * shadow;
    color.rgb = MixFog(color.rgb, fogFactor);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
    
    return color;
}