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
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VSOut
{
    float4 clipPos : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VSOut UnlitPassVertex (VSIn vsi)
{
    VSOut vso;
    UNITY_SETUP_INSTANCE_ID(vsi);
    UNITY_TRANSFER_INSTANCE_ID(vsi, vso);
    
    float4 worldPos = mul(UNITY_MATRIX_M, float4(vsi.pos.xyz, 1.0));
    vso.clipPos = mul(unity_MatrixVP, worldPos);

    return vso;
}

float4 UnlitPassFragment(VSOut psi) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(psi);
    return UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
}