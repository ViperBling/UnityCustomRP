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

struct VSIn
{
    float4 pos : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VSOut
{
    float4 clipPos : SV_POSITION;
    // UNITY_VERTEX_INPUT_INSTANCE_ID
};

VSOut ShadowCasterVS(VSIn vsi)
{
    VSOut vso;
    UNITY_SETUP_INSTANCE_ID(vsi);
    
    float4 worldPos = mul(UNITY_MATRIX_M, float4(vsi.pos.xyz, 1.0));
    vso.clipPos = mul(unity_MatrixVP, worldPos);
    // 避免光源和附近的地方相交出现阴影孔洞，限制顶点位置在近平面外
    
    #if UNITY_REVERSED_Z
    vso.clipPos.z = min(vso.clipPos.z, vso.clipPos.w * UNITY_NEAR_CLIP_VALUE);
    #else
    vso.clipPos.z = max(vso.clipPos.z, vso.clipPos.w * UNITY_NEAR_CLIP_VALUE);
    #endif
    
    return vso;
}

float4 ShadowCasterPS(VSOut psi) : SV_TARGET
{
    // UNITY_SETUP_INSTANCE_ID(psi);
    return 0;
}