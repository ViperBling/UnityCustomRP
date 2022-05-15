#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)

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

    return vso;
}

float4 LitPassFragment(VSOut psi) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(psi);
    float3 worldNormal = normalize(psi.worldNormal);
    float3 albedo = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).rgb;
    float3 diffuse = saturate(dot(worldNormal, float3(0.5, 1.0, -0.3)));

    float3 color = diffuse * albedo;
    return float4(color, 1.0);
}