#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4 unity_PerObjectLightData;
    float4 unity_PerObjectLightIndices;
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

float3 DiffuseLight(int index, float3 normal, float3 worldPos)
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
    diffuse *= spotFade * rangeFade / disSqr;
    
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
    
    return vso;
}

float4 LitPassFragment(VSOut psi) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(psi);
    float3 worldNormal = normalize(psi.worldNormal);
    float3 albedo = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).rgb;
    
    float3 diffuseLight = 0;
    for (int i = 0; i < MAX_VISIBLE_LIGHTS; i++)
    {
        diffuseLight += DiffuseLight(i, worldNormal, psi.worldPos);
    }

    float3 color = diffuseLight * albedo;
    return float4(color, 1.0);
}