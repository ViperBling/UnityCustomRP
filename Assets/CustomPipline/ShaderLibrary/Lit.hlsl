#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4 unity_LightData;
    real4 unity_LightIndices[2];
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)

#define MAX_VISIBLE_LIGHTS 16
CBUFFER_START(_LightBuffer)
    float4 _VisibleLightColors[MAX_VISIBLE_LIGHTS];
    float4 _VisibleLightDirsOrPos[MAX_VISIBLE_LIGHTS];
    float4 _VisibleLightAttenuations[MAX_VISIBLE_LIGHTS];
    float4 _VisibleLightSpotDirections[MAX_VISIBLE_LIGHTS];
CBUFFER_END

CBUFFER_START(_ShadowBuffer)
    float4x4 _WorldToShadowMatrices[MAX_VISIBLE_LIGHTS];
    float4 _ShadowData[MAX_VISIBLE_LIGHTS];
    float4 _ShadowMapSize;
CBUFFER_END

TEXTURE2D_SHADOW(_ShadowMap);
SAMPLER_CMP(sampler_ShadowMap);

float HardShadowAttenuation(float4 shadowPos)
{
    return SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowPos.xyz);
}

float SoftShadowAttenuation(float4 shadowPos)
{
    real tentWeights[9];
    real2 tentUVs[9];
    SampleShadow_ComputeSamples_Tent_5x5(_ShadowMapSize, shadowPos.xy, tentWeights, tentUVs);

    float attenuation = 0;
    for (int i = 0; i < 9; i++)
    {
        attenuation += tentWeights[i] * SAMPLE_TEXTURE2D_SHADOW(
            _ShadowMap, sampler_ShadowMap, float3(tentUVs[i].xy, shadowPos.z));
    }

    return attenuation;
}

float ShadowAttenuation(int index, float3 worldPos)
{
    #if !defined(_SHADOWS_HARD) && !defined(_SHADOWS_SOFT)
        return 1.0;
    #endif
    
    if (_ShadowData[index].x <= 0)
    {
        return 1.0f;
    }
    float4 shadowPos = mul(_WorldToShadowMatrices[index], float4(worldPos.xyz, 1.));
    shadowPos.xyz /= shadowPos.w;

    float attenuation = 0;
    #if defined(_SHADOWS_HARD)
        #if defined(_SHADOWS_SOFT)
            if (_ShadowData[index].y ==  0)
            {
                attenuation = HardShadowAttenuation(shadowPos);
            } else
            {
                attenuation = SoftShadowAttenuation(shadowPos);
            }
        #else
            attenuation = HardShadowAttenuation(shadowPos);
        #endif
    #else
        attenuation = SoftShadowAttenuation(shadowPos);
    #endif
    
    return lerp(1, attenuation, _ShadowData[index].x);
}

float3 DiffuseLight(int index, float3 normal, float3 worldPos, float shadowAttenuation)
{
    float3 lightColor = _VisibleLightColors[index].rgb;
    float4 lightDirOrPos = _VisibleLightDirsOrPos[index];
    float4 lightAtten = _VisibleLightAttenuations[index];
    float3 spotDirection = _VisibleLightSpotDirections[index].xyz;

    float3 lightVector = lightDirOrPos.xyz - worldPos * lightDirOrPos.w;
    float3 lightDirection = normalize(lightVector);
    
    float diffuse = saturate(dot(lightDirection, normal));

    float rangeFade = dot(lightVector, lightVector) * lightAtten.x;
    rangeFade = saturate(1.0 - rangeFade * rangeFade);
    rangeFade *= rangeFade;

    float spotFade = dot(spotDirection, lightDirection);
    spotFade = saturate(spotFade * lightAtten.z + lightAtten.w);

    float disSqr = max(dot(lightVector, lightVector), 0.000001);
    diffuse *= shadowAttenuation * spotFade * rangeFade / disSqr;
    
    return diffuse * lightColor;
}

struct VSIn
{
    float4 pos : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VSOut
{
    float4 clipPos : SV_POSITION;
    float3 worldNormal : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float3 vertexLighting : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VSOut LitPassVertex (VSIn vsi)
{
    VSOut vso;
    UNITY_SETUP_INSTANCE_ID(vsi);
    UNITY_TRANSFER_INSTANCE_ID(vsi, vso);
    
    float4 worldPos = mul(UNITY_MATRIX_M, float4(vsi.pos.xyz, 1.0));
    vso.clipPos = mul(unity_MatrixVP, worldPos);
    vso.worldNormal = mul((float3x3)UNITY_MATRIX_M, vsi.normal);
    vso.worldPos = worldPos.xyz;

    vso.vertexLighting = 0;
    for (int i = 4; i < min(unity_LightData.y, 16); i++)
    {
        int lightIndex = unity_LightIndices[1][i-4];
        vso.vertexLighting += DiffuseLight(lightIndex, vso.worldNormal, vso.worldPos, 1);
    }
    
    return vso;
}

float4 LitPassFragment(VSOut psi) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(psi);
    float3 worldNormal = normalize(psi.worldNormal);
    float3 albedo = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).rgb;
    

    float3 diffuseLight = psi.vertexLighting;
    for (int i = 0; i < min(unity_LightData.y, 4); i++)
    {
        int lightIndex = unity_LightIndices[0][i];
        float shadowAttenuation = ShadowAttenuation(lightIndex, psi.worldPos);
        diffuseLight += DiffuseLight(lightIndex, worldNormal, psi.worldPos, shadowAttenuation);
    }

    float3 color = diffuseLight * albedo;
    return float4(color, 1.0);
}